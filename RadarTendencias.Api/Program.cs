using DbUp;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddHttpClient("JikanClient", client =>
{
    client.BaseAddress = new Uri("https://api.jikan.moe/v4/");
});

builder.Services.AddHttpClient("TmdbClient", client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["TmdbApiKey"]}");
});

builder.Services.AddHttpClient("RedditClient", client =>
{
    client.BaseAddress = new Uri("https://www.reddit.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "windows:radartendenciasapi:v1.0 (by /u/erick)");
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
EnsureDatabase.For.SqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

upgrader.PerformUpgrade();

var app = builder.Build();
app.UseCors();

app.MapGet("/franquias", async (IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var franquias = await connection.QueryAsync("SELECT * FROM Franquias");
    return Results.Ok(franquias);
});

app.MapGet("/monitoramento/dashboard", async (IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = @"
        SELECT f.FranquiaID, f.Nome, f.CategoriaID, m.HypeScore, m.VolumeMencoes, m.SentimentoPositivo,
               (SELECT STRING_AGG(t.Nome, ', ') FROM FranquiaTags ft INNER JOIN Tags t ON ft.TagID = t.TagID WHERE ft.FranquiaID = f.FranquiaID) as TagsString
        FROM Franquias f
        INNER JOIN (
            SELECT FranquiaID, HypeScore, VolumeMencoes, SentimentoPositivo,
                   ROW_NUMBER() OVER(PARTITION BY FranquiaID ORDER BY DataMedicao DESC) as rn
            From MonitoramentoHype
        ) m ON m.FranquiaID = f.FranquiaID AND m.rn = 1
        ORDER BY m.HypeScore DESC";
    
    var dados = await connection.QueryAsync(sql);
    return Results.Ok(dados);
});

app.MapPost("/franquias/sync", async (IConfiguration config, SyncFranquiaDTO dto) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    await connection.OpenAsync();
    using var transaction = connection.BeginTransaction();
    try
    {
        var sqlCheck = "SELECT FranquiaID FROM Franquias WHERE Nome = @Nome";
        var id = await connection.QuerySingleOrDefaultAsync<int?>(sqlCheck, new { dto.Nome }, transaction);
        if (id == null || id == 0)
        {
            var sqlInsert = "INSERT INTO Franquias (Nome, CategoriaID, Ativo, ExternalID, Sinopse, ImagemUrl) OUTPUT INSERTED.FranquiaID VALUES (@Nome, @CategoriaID, 1, @ExternalID, @Sinopse, @ImagemUrl)";
            id = await connection.QuerySingleAsync<int>(sqlInsert, dto, transaction);
        }
        else
        {
            var sqlUpdate = "UPDATE Franquias SET ExternalID = @ExternalID, Sinopse = @Sinopse, ImagemUrl = @ImagemUrl WHERE FranquiaID = @id";
            await connection.ExecuteAsync(sqlUpdate, new { dto.ExternalID, dto.Sinopse, dto.ImagemUrl, id }, transaction);
        }
        foreach (var tagName in dto.Tags)
        {
            var sqlTag = "SELECT TagID FROM Tags WHERE Nome = @Nome";
            var tagId = await connection.QuerySingleOrDefaultAsync<int?>(sqlTag, new { Nome = tagName }, transaction);
            if (tagId == null || tagId == 0)
            {
                var sqlInsertTag = "INSERT INTO Tags (Nome) OUTPUT INSERTED.TagID VALUES (@Nome)";
                tagId = await connection.QuerySingleAsync<int>(sqlInsertTag, new { Nome = tagName }, transaction);
            }
            var sqlRelCheck = "SELECT COUNT(1) FROM FranquiaTags WHERE FranquiaID = @FId AND TagID = @TId";
            var exists = await connection.ExecuteScalarAsync<int>(sqlRelCheck, new { FId = id, TId = tagId }, transaction);
            if (exists == 0)
            {
                var sqlInsertRel = "INSERT INTO FranquiaTags (FranquiaID, TagID) VALUES (@FId, @TId)";
                await connection.ExecuteAsync(sqlInsertRel, new { FId = id, TId = tagId }, transaction);
            }
        }
        transaction.Commit();
        return Results.Ok(new { FranquiaID = id });
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
});

app.MapGet("/pesquisa", async (IConfiguration config, IHttpClientFactory httpClientFactory, string q) =>
{
    var jikan = httpClientFactory.CreateClient("JikanClient");
    var tmdb = httpClientFactory.CreateClient("TmdbClient");
    var syncList = new List<SyncFranquiaDTO>();
    var termo = Uri.EscapeDataString(q);
    bool jikanSucesso = false;

    try
    {
        var animeRes = await jikan.GetFromJsonAsync<JikanSearchResponse>($"anime?q={termo}&limit=25");
        if (animeRes?.Data != null)
        {
            foreach (var a in animeRes.Data)
            {
                if (string.IsNullOrWhiteSpace(a.Title)) continue;
                syncList.Add(new SyncFranquiaDTO { 
                    Nome = a.Title, 
                    CategoriaID = 1, 
                    Tags = a.Genres?.Select(g => g.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new(), 
                    ExternalID = a.MalId, 
                    Sinopse = a.Synopsis, 
                    ImagemUrl = a.Images?.Jpg?.ImageUrl, 
                    ScoreInicial = a.Score 
                });
            }
            jikanSucesso = true;
        }
    }
    catch 
    {
        jikanSucesso = false;
    }

    if (!jikanSucesso)
    {
        try
        {
            using var anilistClient = new HttpClient();
            anilistClient.BaseAddress = new Uri("https://graphql.anilist.co");
            
            var queryGraphql = new {
                query = @"query ($search: String) { Page(perPage: 15) { media(search: $search, type: ANIME) { id title { romaji english } coverImage { large } description genres averageScore } } }",
                variables = new { search = q }
            };

            var anilistResponse = await anilistClient.PostAsJsonAsync("", queryGraphql);
            if (anilistResponse.IsSuccessStatusCode)
            {
                var anilistResult = await anilistResponse.Content.ReadFromJsonAsync<AnilistResponse>();
                if (anilistResult?.Data?.Page?.Media != null)
                {
                    foreach (var media in anilistResult.Data.Page.Media)
                    {
                        var tituloAnilist = media.Title?.English ?? media.Title?.Romaji;
                        if (string.IsNullOrWhiteSpace(tituloAnilist)) continue;

                        syncList.Add(new SyncFranquiaDTO {
                            Nome = tituloAnilist,
                            CategoriaID = 1,
                            Tags = media.Genres ?? new(),
                            ExternalID = media.Id,
                            Sinopse = media.Description,
                            ImagemUrl = media.CoverImage?.Large,
                            ScoreInicial = media.AverageScore.HasValue ? media.AverageScore.Value / 10.0 : 0
                        });
                    }
                }
            }
        }
        catch { }
    }

    try
    {
        var mangaRes = await jikan.GetFromJsonAsync<JikanSearchResponse>($"manga?q={termo}&limit=15");
        if (mangaRes?.Data != null)
        {
            foreach (var m in mangaRes.Data)
            {
                if (string.IsNullOrWhiteSpace(m.Title)) continue;
                syncList.Add(new SyncFranquiaDTO { 
                    Nome = m.Title, 
                    CategoriaID = 3, 
                    Tags = m.Genres?.Select(g => g.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new(), 
                    ExternalID = m.MalId, 
                    Sinopse = m.Synopsis, 
                    ImagemUrl = m.Images?.Jpg?.ImageUrl, 
                    ScoreInicial = m.Score 
                });
            }
        }
    }
    catch { }

    try
    {
        var tmdbRes = await tmdb.GetFromJsonAsync<TmdbSearchResponse>($"search/multi?query={termo}&language=pt-BR");
        if (tmdbRes?.Results != null)
        {
            foreach (var t in tmdbRes.Results.Take(15))
            {
                var title = !string.IsNullOrWhiteSpace(t.Title) ? t.Title : t.Name;
                if (string.IsNullOrWhiteSpace(title)) continue;
                syncList.Add(new SyncFranquiaDTO { 
                    Nome = title, 
                    CategoriaID = 2, 
                    Tags = new List<string> { t.MediaType == "tv" ? "Série" : "Filme" }, 
                    ExternalID = t.Id, 
                    Sinopse = t.Overview, 
                    ImagemUrl = !string.IsNullOrEmpty(t.PosterPath) ? $"https://image.tmdb.org/t/p/w500{t.PosterPath}" : null, 
                    ScoreInicial = t.VoteAverage 
                });
            }
        }
    }
    catch { }

    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var resultados = new List<object>();

    foreach (var dto in syncList)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) continue;
        var sqlCheck = "SELECT FranquiaID FROM Franquias WHERE Nome = @Nome";
        var id = await connection.QuerySingleOrDefaultAsync<int?>(sqlCheck, new { dto.Nome });
        
        if (id == null || id == 0)
        {
            var sqlInsert = "INSERT INTO Franquias (Nome, CategoriaID, Ativo, ExternalID, Sinopse, ImagemUrl) OUTPUT INSERTED.FranquiaID VALUES (@Nome, @CategoriaID, 1, @ExternalID, @Sinopse, @ImagemUrl)";
            id = await connection.QuerySingleAsync<int>(sqlInsert, dto);
            
            var notaNormalizada = (decimal)((dto.ScoreInicial ?? 0) * 10);
            if (notaNormalizada == 0) notaNormalizada = 50.0m;
            await connection.ExecuteAsync("INSERT INTO MonitoramentoHype (FranquiaID, HypeScore, VolumeMencoes, SentimentoPositivo) VALUES (@FId, @HS, 0, 50.0)", new { FId = id, HS = notaNormalizada });

            foreach (var tagName in dto.Tags)
            {
                var sqlTag = "SELECT TagID FROM Tags WHERE Nome = @Nome";
                var tagId = await connection.QuerySingleOrDefaultAsync<int?>(sqlTag, new { Nome = tagName });
                if (tagId == null || tagId == 0)
                {
                    tagId = await connection.QuerySingleAsync<int>("INSERT INTO Tags (Nome) OUTPUT INSERTED.TagID VALUES (@Nome)", new { Nome = tagName });
                }
                await connection.ExecuteAsync("INSERT INTO FranquiaTags (FranquiaID, TagID) VALUES (@FId, @TId)", new { FId = id, TId = tagId });
            }
        }
        else
        {
            await connection.ExecuteAsync("UPDATE Franquias SET ExternalID = @ExternalID, Sinopse = @Sinopse, ImagemUrl = @ImagemUrl WHERE FranquiaID = @id", new { dto.ExternalID, dto.Sinopse, dto.ImagemUrl, id });
        }
        
        resultados.Add(new { FranquiaID = id, Nome = dto.Nome, CategoriaID = dto.CategoriaID, ImagemUrl = dto.ImagemUrl, Tags = dto.Tags });
    }

    return Results.Ok(resultados.OrderBy(r => ((dynamic)r).CategoriaID).ToList());
});

app.MapGet("/franquias/{id}/detalhes", async (IConfiguration config, int id) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sqlFranquia = @"
        SELECT f.*, 
               (SELECT STRING_AGG(t.Nome, ', ') FROM FranquiaTags ft INNER JOIN Tags t ON ft.TagID = t.TagID WHERE ft.FranquiaID = f.FranquiaID) as TagsString
        FROM Franquias f WHERE f.FranquiaID = @Id";
    
    var franquia = await connection.QuerySingleOrDefaultAsync(sqlFranquia, new { Id = id });
    if (franquia == null) return Results.NotFound();

    var sqlHistorico = "SELECT TOP 30 HypeScore, VolumeMencoes, SentimentoPositivo, DataMedicao FROM MonitoramentoHype WHERE FranquiaID = @Id ORDER BY DataMedicao DESC";
    var historico = await connection.QueryAsync(sqlHistorico, new { Id = id });

    return Results.Ok(new { Detalhes = franquia, Historico = historico });
});

app.MapGet("/franquias/{id}/personagens", async (IConfiguration config, IHttpClientFactory httpClientFactory, int id) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sqlFranquia = @"SELECT ExternalID, CategoriaID, (SELECT STRING_AGG(t.Nome, ', ') FROM FranquiaTags ft INNER JOIN Tags t ON ft.TagID = t.TagID WHERE ft.FranquiaID = Franquias.FranquiaID) as TagsString FROM Franquias WHERE FranquiaID = @Id";
    var franquia = await connection.QuerySingleOrDefaultAsync(sqlFranquia, new { Id = id });
    
    if (franquia == null || franquia.ExternalID == null) return Results.Ok(new List<PersonagemDTO>());

    var personagens = new List<PersonagemDTO>();

    try 
    {
        if (franquia.CategoriaID == 1 || franquia.CategoriaID == 3)
        {
            var jikan = httpClientFactory.CreateClient("JikanClient");
            var tipo = franquia.CategoriaID == 1 ? "anime" : "manga";
            var response = await jikan.GetFromJsonAsync<JikanCharactersResponse>($"{tipo}/{franquia.ExternalID}/characters");
            if (response?.Data != null)
            {
                personagens = response.Data
                    .Where(c => c.Character?.Images?.Jpg?.ImageUrl != null && !c.Character.Images.Jpg.ImageUrl.Contains("questionmark"))
                    .Take(12)
                    .Select(c => new PersonagemDTO { Nome = c.Character?.Name ?? "", Papel = c.Role ?? "Personagem", ImagemUrl = c.Character?.Images?.Jpg?.ImageUrl ?? "" }).ToList();
            }
        }
        else if (franquia.CategoriaID == 2)
        {
            var tmdb = httpClientFactory.CreateClient("TmdbClient");
            var tipo = (franquia.TagsString != null && franquia.TagsString.Contains("Série")) ? "tv" : "movie";
            var response = await tmdb.GetFromJsonAsync<TmdbCreditsResponse>($"{tipo}/{franquia.ExternalID}/credits?language=pt-BR");
            if (response?.Cast != null)
            {
                personagens = response.Cast
                    .Where(c => !string.IsNullOrEmpty(c.ProfilePath))
                    .Take(12)
                    .Select(c => new PersonagemDTO { Nome = c.Character ?? c.Name ?? "", Papel = "Ator/Personagem", ImagemUrl = $"https://image.tmdb.org/t/p/w200{c.ProfilePath}" }).ToList();
            }
        }
    } 
    catch { }

    return Results.Ok(personagens);
});

app.MapGet("/franquias/{id}/comunidade", async (IConfiguration config, IHttpClientFactory httpClientFactory, int id) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sqlFranquia = @"SELECT Nome, CategoriaID, ExternalID, (SELECT STRING_AGG(t.Nome, ', ') FROM FranquiaTags ft INNER JOIN Tags t ON ft.TagID = t.TagID WHERE ft.FranquiaID = Franquias.FranquiaID) as TagsString FROM Franquias WHERE FranquiaID = @Id";
    var franquia = await connection.QuerySingleOrDefaultAsync<FranquiaDbResult>(sqlFranquia, new { Id = id });
    
    if (franquia == null) return Results.Ok(new List<object>());

    var feedbacks = new List<ComunidadeFeedbackDTO>();

    try
    {
        var redditClient = httpClientFactory.CreateClient("RedditClient");
        var query = Uri.EscapeDataString(franquia.Nome);
        var redditResponse = await redditClient.GetFromJsonAsync<RedditResponse>($"search.json?q={query}&sort=hot&limit=3");
        if (redditResponse?.Data?.Children != null)
        {
            feedbacks.AddRange(redditResponse.Data.Children.Select(c => new ComunidadeFeedbackDTO { Fonte = "Reddit", Autor = c.Data?.Author ?? "Usuário", Titulo = c.Data?.Title ?? "", Texto = c.Data?.Selftext ?? "", Url = $"https://reddit.com{c.Data?.Permalink}" }).Where(p => !string.IsNullOrWhiteSpace(p.Titulo)));
        }
    }
    catch { }

    try
    {
        using var ytClient = new HttpClient();
        var ytKey = config["YouTubeApiKey"];
        var ytQuery = Uri.EscapeDataString(franquia.Nome + " review trailer");
        var ytRes = await ytClient.GetFromJsonAsync<YoutubeSearchResponse>($"https://www.googleapis.com/youtube/v3/search?part=snippet&q={ytQuery}&type=video&key={ytKey}&maxResults=2");
        if (ytRes?.Items != null)
        {
            feedbacks.AddRange(ytRes.Items.Select(v => new ComunidadeFeedbackDTO { Fonte = "YouTube", Autor = v.Snippet?.ChannelTitle ?? "Canal", Titulo = v.Snippet?.Title ?? "", Texto = v.Snippet?.Description ?? "", Url = $"https://youtube.com/watch?v={v.Id?.VideoId}" }));
        }
    }
    catch { }

    if (franquia.ExternalID != null)
    {
        try
        {
            if (franquia.CategoriaID == 1 || franquia.CategoriaID == 3)
            {
                var jikanClient = httpClientFactory.CreateClient("JikanClient");
                var tipo = franquia.CategoriaID == 1 ? "anime" : "manga";
                var jikanResponse = await jikanClient.GetFromJsonAsync<JikanReviewsResponse>($"{tipo}/{franquia.ExternalID}/reviews");
                if (jikanResponse?.Data != null)
                {
                    feedbacks.AddRange(jikanResponse.Data.Take(2).Select(r => new ComunidadeFeedbackDTO { Fonte = "MyAnimeList", Autor = r.User?.Username ?? "Reviewer", Titulo = "Avaliação da Comunidade", Texto = r.ReviewText ?? "", Url = r.Url ?? "" }));
                }
            }
            else if (franquia.CategoriaID == 2)
            {
                var tmdbClient = httpClientFactory.CreateClient("TmdbClient");
                var tipo = (franquia.TagsString != null && franquia.TagsString.Contains("Série")) ? "tv" : "movie";
                var tmdbResponse = await tmdbClient.GetFromJsonAsync<TmdbReviewsResponse>($"{tipo}/{franquia.ExternalID}/reviews");
                if (tmdbResponse?.Results != null)
                {
                    feedbacks.AddRange(tmdbResponse.Results.Take(2).Select(r => new ComunidadeFeedbackDTO { Fonte = "TMDB", Autor = r.Author ?? "Crítico", Titulo = "Review TMDB", Texto = r.Content ?? "", Url = r.Url ?? "" }));
                }
            }
        }
        catch { }
    }

    return Results.Ok(feedbacks.OrderBy(x => Guid.NewGuid()).ToList());
});

app.MapPost("/monitoramento", async (IConfiguration config, MonitoramentoDTO dto) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = @"INSERT INTO MonitoramentoHype (FranquiaID, HypeScore, VolumeMencoes, SentimentoPositivo) 
                VALUES (@FranquiaID, @HypeScore, @VolumeMencoes, @SentimentoPositivo)";
    await connection.ExecuteAsync(sql, dto);
    return Results.Created();
});

app.MapGet("/fluxos", async (IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var fluxos = await connection.QueryAsync<FluxoDTO>("SELECT * FROM Fluxos");
    foreach(var fluxo in fluxos)
    {
        fluxo.Nodes = (await connection.QueryAsync<NodeDTO>("SELECT * FROM FluxoNodes WHERE FluxoID = @Id", new { Id = fluxo.FluxoID })).ToList();
        fluxo.Connections = (await connection.QueryAsync<ConnectionDTO>("SELECT * FROM FluxoConexoes WHERE FluxoID = @Id", new { Id = fluxo.FluxoID })).ToList();
    }
    return Results.Ok(fluxos);
});

app.MapPost("/fluxos", async (IConfiguration config, FluxoDTO fluxo) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    await connection.OpenAsync();
    using var transaction = connection.BeginTransaction();
    try 
    {
        var sqlInsertFluxo = "INSERT INTO Fluxos (Nome) OUTPUT INSERTED.FluxoID VALUES (@Nome)";
        var fluxoId = await connection.QuerySingleAsync<int>(sqlInsertFluxo, new { fluxo.Nome }, transaction);
        foreach (var node in fluxo.Nodes)
        {
            var sqlInsertNode = "INSERT INTO FluxoNodes (NodeID, FluxoID, Tipo, Label, PosX, PosY) VALUES (@NodeID, @FluxoID, @Tipo, @Label, @PosX, @PosY)";
            await connection.ExecuteAsync(sqlInsertNode, new { node.NodeID, FluxoID = fluxoId, node.Tipo, node.Label, node.PosX, node.PosY }, transaction);
        }
        foreach (var conn in fluxo.Connections)
        {
            var sqlInsertConn = "INSERT INTO FluxoConexoes (ConnectionID, FluxoID, SourceNodeID, SourcePortID, TargetNodeID, TargetPortID) VALUES (@ConnectionID, @FluxoID, @SourceNodeID, @SourcePortID, @TargetNodeID, @TargetPortID)";
            await connection.ExecuteAsync(sqlInsertConn, new { conn.ConnectionID, FluxoID = fluxoId, conn.SourceNodeID, conn.SourcePortID, conn.TargetNodeID, conn.TargetPortID }, transaction);
        }
        transaction.Commit();
        return Results.Created($"/fluxos/{fluxoId}", new { FluxoID = fluxoId });
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
});

app.MapGet("/alertas", async (IConfiguration config) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = @"SELECT a.*, f.Nome as FranquiaNome FROM Alertas a 
                INNER JOIN Franquias f ON a.FranquiaID = f.FranquiaID 
                ORDER BY a.DataAlerta DESC";
    var alertas = await connection.QueryAsync(sql);
    return Results.Ok(alertas);
});

app.MapPost("/alertas", async (IConfiguration config, AlertaDTO dto) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = "INSERT INTO Alertas (FranquiaID, FluxoID, Mensagem) VALUES (@FranquiaID, @FluxoID, @Mensagem)";
    await connection.ExecuteAsync(sql, dto);
    return Results.Created();
});

app.MapPut("/alertas/{id}/ler", async (IConfiguration config, int id) =>
{
    using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    var sql = "UPDATE Alertas SET Lido = 1 WHERE AlertaID = @Id";
    await connection.ExecuteAsync(sql, new { Id = id });
    return Results.NoContent();
});

app.Run();

public class MonitoramentoDTO { public int FranquiaID { get; set; } public decimal HypeScore { get; set; } public int VolumeMencoes { get; set; } public decimal SentimentoPositivo { get; set; } }
public class SyncFranquiaDTO { public string Nome { get; set; } = string.Empty; public int CategoriaID { get; set; } public List<string> Tags { get; set; } = new(); public int? ExternalID { get; set; } public string? Sinopse { get; set; } public string? ImagemUrl { get; set; } public double? ScoreInicial { get; set; } }
public class FluxoDTO { public int FluxoID { get; set; } public string Nome { get; set; } = string.Empty; public List<NodeDTO> Nodes { get; set; } = new(); public List<ConnectionDTO> Connections { get; set; } = new(); }
public class NodeDTO { public string NodeID { get; set; } = string.Empty; public string Tipo { get; set; } = string.Empty; public string Label { get; set; } = string.Empty; public int PosX { get; set; } public int PosY { get; set; } }
public class ConnectionDTO { public string ConnectionID { get; set; } = string.Empty; public string SourceNodeID { get; set; } = string.Empty; public string SourcePortID { get; set; } = string.Empty; public string TargetNodeID { get; set; } = string.Empty; public string TargetPortID { get; set; } = string.Empty; }
public class AlertaDTO { public int FranquiaID { get; set; } public int FluxoID { get; set; } public string Mensagem { get; set; } = string.Empty; }
public class PersonagemDTO { public string Nome { get; set; } = string.Empty; public string Papel { get; set; } = string.Empty; public string ImagemUrl { get; set; } = string.Empty; }
public class JikanSearchResponse { [System.Text.Json.Serialization.JsonPropertyName("data")] public List<JikanAnime>? Data { get; set; } }
public class JikanCharactersResponse { [System.Text.Json.Serialization.JsonPropertyName("data")] public List<JikanCharacterData>? Data { get; set; } }
public class JikanCharacterData { [System.Text.Json.Serialization.JsonPropertyName("character")] public JikanCharacter? Character { get; set; } [System.Text.Json.Serialization.JsonPropertyName("role")] public string? Role { get; set; } }
public class JikanCharacter { [System.Text.Json.Serialization.JsonPropertyName("name")] public string? Name { get; set; } [System.Text.Json.Serialization.JsonPropertyName("images")] public JikanCharacterImages? Images { get; set; } }
public class JikanCharacterImages { [System.Text.Json.Serialization.JsonPropertyName("jpg")] public JikanCharacterJpg? Jpg { get; set; } }
public class JikanCharacterJpg { [System.Text.Json.Serialization.JsonPropertyName("image_url")] public string? ImageUrl { get; set; } }
public class TmdbSearchResponse { [System.Text.Json.Serialization.JsonPropertyName("results")] public List<TmdbMedia>? Results { get; set; } }
public class TmdbCreditsResponse { [System.Text.Json.Serialization.JsonPropertyName("cast")] public List<TmdbCast>? Cast { get; set; } }
public class TmdbCast { [System.Text.Json.Serialization.JsonPropertyName("name")] public string? Name { get; set; } [System.Text.Json.Serialization.JsonPropertyName("character")] public string? Character { get; set; } [System.Text.Json.Serialization.JsonPropertyName("profile_path")] public string? ProfilePath { get; set; } }
public class JikanAnime { [System.Text.Json.Serialization.JsonPropertyName("mal_id")] public int MalId { get; set; } [System.Text.Json.Serialization.JsonPropertyName("title")] public string? Title { get; set; } [System.Text.Json.Serialization.JsonPropertyName("score")] public double? Score { get; set; } [System.Text.Json.Serialization.JsonPropertyName("members")] public int Members { get; set; } [System.Text.Json.Serialization.JsonPropertyName("synopsis")] public string? Synopsis { get; set; } [System.Text.Json.Serialization.JsonPropertyName("genres")] public List<JikanGenre>? Genres { get; set; } [System.Text.Json.Serialization.JsonPropertyName("images")] public JikanImages? Images { get; set; } }
public class JikanGenre { [System.Text.Json.Serialization.JsonPropertyName("name")] public string? Name { get; set; } }
public class JikanImages { [System.Text.Json.Serialization.JsonPropertyName("jpg")] public JikanJpg? Jpg { get; set; } }
public class JikanJpg { [System.Text.Json.Serialization.JsonPropertyName("image_url")] public string? ImageUrl { get; set; } }
public class TmdbMedia { [System.Text.Json.Serialization.JsonPropertyName("id")] public int Id { get; set; } [System.Text.Json.Serialization.JsonPropertyName("title")] public string? Title { get; set; } [System.Text.Json.Serialization.JsonPropertyName("name")] public string? Name { get; set; } [System.Text.Json.Serialization.JsonPropertyName("vote_average")] public double VoteAverage { get; set; } [System.Text.Json.Serialization.JsonPropertyName("vote_count")] public int VoteCount { get; set; } [System.Text.Json.Serialization.JsonPropertyName("overview")] public string? Overview { get; set; } [System.Text.Json.Serialization.JsonPropertyName("media_type")] public string? MediaType { get; set; } [System.Text.Json.Serialization.JsonPropertyName("poster_path")] public string? PosterPath { get; set; } }
public class RedditResponse { [System.Text.Json.Serialization.JsonPropertyName("data")] public RedditData? Data { get; set; } }
public class RedditData { [System.Text.Json.Serialization.JsonPropertyName("children")] public List<RedditChild>? Children { get; set; } }
public class RedditChild { [System.Text.Json.Serialization.JsonPropertyName("data")] public RedditPostData? Data { get; set; } }
public class ComunidadeFeedbackDTO { public string Fonte { get; set; } = string.Empty; public string Autor { get; set; } = string.Empty; public string Titulo { get; set; } = string.Empty; public string Texto { get; set; } = string.Empty; public string Url { get; set; } = string.Empty; }
public class RedditPostData { [System.Text.Json.Serialization.JsonPropertyName("title")] public string? Title { get; set; } [System.Text.Json.Serialization.JsonPropertyName("selftext")] public string? Selftext { get; set; } [System.Text.Json.Serialization.JsonPropertyName("author")] public string? Author { get; set; } [System.Text.Json.Serialization.JsonPropertyName("permalink")] public string? Permalink { get; set; } }
public class JikanReviewsResponse { [System.Text.Json.Serialization.JsonPropertyName("data")] public List<JikanReview>? Data { get; set; } }
public class JikanReview { [System.Text.Json.Serialization.JsonPropertyName("review")] public string? ReviewText { get; set; } [System.Text.Json.Serialization.JsonPropertyName("url")] public string? Url { get; set; } [System.Text.Json.Serialization.JsonPropertyName("user")] public JikanUser? User { get; set; } }
public class JikanUser { [System.Text.Json.Serialization.JsonPropertyName("username")] public string? Username { get; set; } }
public class TmdbReviewsResponse { [System.Text.Json.Serialization.JsonPropertyName("results")] public List<TmdbReview>? Results { get; set; } }
public class TmdbReview { [System.Text.Json.Serialization.JsonPropertyName("content")] public string? Content { get; set; } [System.Text.Json.Serialization.JsonPropertyName("author")] public string? Author { get; set; } [System.Text.Json.Serialization.JsonPropertyName("url")] public string? Url { get; set; } }
public class FranquiaDbResult { public string Nome { get; set; } = string.Empty; public int CategoriaID { get; set; } public int? ExternalID { get; set; } public string? TagsString { get; set; } }
public class YoutubeSearchResponse { [System.Text.Json.Serialization.JsonPropertyName("items")] public List<YoutubeItem>? Items { get; set; } }
public class YoutubeItem { [System.Text.Json.Serialization.JsonPropertyName("id")] public YoutubeId? Id { get; set; } [System.Text.Json.Serialization.JsonPropertyName("snippet")] public YoutubeSnippet? Snippet { get; set; } }
public class YoutubeId { [System.Text.Json.Serialization.JsonPropertyName("videoId")] public string? VideoId { get; set; } }
public class YoutubeSnippet { [System.Text.Json.Serialization.JsonPropertyName("title")] public string? Title { get; set; } [System.Text.Json.Serialization.JsonPropertyName("description")] public string? Description { get; set; } [System.Text.Json.Serialization.JsonPropertyName("channelTitle")] public string? ChannelTitle { get; set; } }

public class AnilistResponse { [System.Text.Json.Serialization.JsonPropertyName("data")] public AnilistData? Data { get; set; } }
public class AnilistData { [System.Text.Json.Serialization.JsonPropertyName("Page")] public AnilistPage? Page { get; set; } }
public class AnilistPage { [System.Text.Json.Serialization.JsonPropertyName("media")] public List<AnilistMedia>? Media { get; set; } }
public class AnilistMedia { [System.Text.Json.Serialization.JsonPropertyName("id")] public int Id { get; set; } [System.Text.Json.Serialization.JsonPropertyName("title")] public AnilistTitle? Title { get; set; } [System.Text.Json.Serialization.JsonPropertyName("coverImage")] public AnilistCoverImage? CoverImage { get; set; } [System.Text.Json.Serialization.JsonPropertyName("description")] public string? Description { get; set; } [System.Text.Json.Serialization.JsonPropertyName("genres")] public List<string>? Genres { get; set; } [System.Text.Json.Serialization.JsonPropertyName("averageScore")] public double? AverageScore { get; set; } }
public class AnilistTitle { [System.Text.Json.Serialization.JsonPropertyName("romaji")] public string? Romaji { get; set; } [System.Text.Json.Serialization.JsonPropertyName("english")] public string? English { get; set; } }
public class AnilistCoverImage { [System.Text.Json.Serialization.JsonPropertyName("large")] public string? Large { get; set; } }
