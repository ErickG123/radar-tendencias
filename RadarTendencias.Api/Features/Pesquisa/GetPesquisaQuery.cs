using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Pesquisa;

public class GetPesquisaQuery : IRequest<IResult>
{
    public string Q { get; set; }
}

public class GetPesquisaHandler : IRequestHandler<GetPesquisaQuery, IResult>
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public GetPesquisaHandler(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    
    private string LimparHtml(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        return Regex.Replace(texto, "<.*?>", string.Empty);
    }

    public async Task<IResult> Handle(GetPesquisaQuery request, CancellationToken cancellationToken)
    {

    var jikan = _httpClientFactory.CreateClient("JikanClient");
    var tmdb = _httpClientFactory.CreateClient("TmdbClient");
    var syncList = new List<SyncFranquiaDTO>();
    var termo = Uri.EscapeDataString(request.Q);
    bool jikanSucesso = false;

    try
    {
        var animeRes = await jikan.GetFromJsonAsync<JikanSearchResponse>($"anime?request.Q={termo}&limit=25");
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
                    Sinopse = LimparHtml(a.Synopsis), 
                    ImagemUrl = a.Images?.Jpg?.ImageUrl, 
                    ScoreInicial = a.Score,
                    Fonte = "MyAnimeList"
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
                variables = new { search = request.Q }
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
                            Sinopse = LimparHtml(media.Description?.ToString()),
                            ImagemUrl = media.CoverImage?.Large,
                            ScoreInicial = media.AverageScore.HasValue ? media.AverageScore.Value / 10.0 : 0,
                            Fonte = "AniList"
                        });
                    }
                }
            }
        }
        catch { }
    }

    try
    {
        var mangaRes = await jikan.GetFromJsonAsync<JikanSearchResponse>($"manga?request.Q={termo}&limit=15");
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
                    Sinopse = LimparHtml(m.Synopsis), 
                    ImagemUrl = m.Images?.Jpg?.ImageUrl, 
                    ScoreInicial = m.Score,
                    Fonte = "MyAnimeList (Mangá)"
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
                    Sinopse = LimparHtml(t.Overview), 
                    ImagemUrl = !string.IsNullOrEmpty(t.PosterPath) ? $"https://image.tmdb.org/t/p/w500{t.PosterPath}" : null, 
                    ScoreInicial = t.VoteAverage,
                    Fonte = "TMDB"
                });
            }
        }
    }
    catch { }

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
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
        
        resultados.Add(new { FranquiaID = id, Nome = dto.Nome, CategoriaID = dto.CategoriaID, ImagemUrl = dto.ImagemUrl, Tags = dto.Tags, Fonte = dto.Fonte });
    }

    return Results.Ok(resultados);

    }
}
