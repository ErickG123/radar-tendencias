using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RadarTendencias.Worker;

public class Worker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public Worker(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessarJikanAsync(stoppingToken);
        await ProcessarTmdbAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessarJikanAsync(stoppingToken);
            await ProcessarTmdbAsync(stoppingToken);
        }
    }

    private async Task<decimal> AnalisarSentimentoAsync(string? texto, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(texto)) return 50.0m;
        try
        {
            var nlpClient = _httpClientFactory.CreateClient("NlpClient");
            var response = await nlpClient.PostAsJsonAsync("/analyze", new { text = texto }, stoppingToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NlpResponse>(cancellationToken: stoppingToken);
                if (result != null) return (decimal)result.Score;
            }
        }
        catch { }
        return 50.0m;
    }

    private async Task<string> BuscarTextosComunidadeAsync(string franquiaNome, int categoriaId, int? externalId, string tagsString, CancellationToken stoppingToken)
    {
        var textos = new List<string>();
        var configuration = _httpClientFactory.CreateClient().BaseAddress == null ? new ConfigurationBuilder().AddJsonFile("appsettings.json").Build() : null;
        var ytKey = configuration?["YouTubeApiKey"] ?? Environment.GetEnvironmentVariable("YouTubeApiKey");

        try
        {
            var redditClient = _httpClientFactory.CreateClient("RedditClient");
            var query = Uri.EscapeDataString(franquiaNome);
            var redditResponse = await redditClient.GetFromJsonAsync<RedditResponse>($"search.json?q={query}&sort=hot&limit=3", stoppingToken);
            if (redditResponse?.Data?.Children != null)
            {
                textos.AddRange(redditResponse.Data.Children.Select(c => $"{c.Data?.Title} {c.Data?.Selftext}"));
            }
        }
        catch { }

        try
        {
            if (!string.IsNullOrEmpty(ytKey))
            {
                using var ytClient = new HttpClient();
                var ytQuery = Uri.EscapeDataString(franquiaNome + " review trailer");
                var ytRes = await ytClient.GetFromJsonAsync<YoutubeSearchResponse>($"https://www.googleapis.com/youtube/v3/search?part=snippet&q={ytQuery}&type=video&key={ytKey}&maxResults=2", stoppingToken);
                if (ytRes?.Items != null)
                {
                    textos.AddRange(ytRes.Items.Select(v => $"{v.Snippet?.Title} {v.Snippet?.Description}"));
                }
            }
        }
        catch { }

        if (externalId.HasValue)
        {
            try
            {
                if (categoriaId == 1 || categoriaId == 3)
                {
                    using var anilistClient = new HttpClient();
                    anilistClient.BaseAddress = new Uri("https://graphql.anilist.co");
                    
                    var queryGraphql = new {
                        query = @"query ($mediaId: Int) { Page(perPage: 3) { threads(mediaId: $mediaId, sort: [REPLIES_DESC]) { title body } } }",
                        variables = new { mediaId = externalId.Value }
                    };

                    var anilistRes = await anilistClient.PostAsJsonAsync("", queryGraphql, stoppingToken);
                    if (anilistRes.IsSuccessStatusCode)
                    {
                        var anilistData = await anilistRes.Content.ReadFromJsonAsync<AnilistThreadsResponse>(cancellationToken: stoppingToken);
                        if (anilistData?.Data?.Page?.Threads != null)
                        {
                            textos.AddRange(anilistData.Data.Page.Threads.Select(t => $"{t.Title} {t.Body}"));
                        }
                    }
                }

                if (categoriaId == 1 || categoriaId == 3)
                {
                    var jikanClient = _httpClientFactory.CreateClient("JikanClient");
                    var tipo = categoriaId == 1 ? "anime" : "manga";
                    var jikanResponse = await jikanClient.GetFromJsonAsync<JikanReviewsResponse>($"{tipo}/{externalId}/reviews", stoppingToken);
                    if (jikanResponse?.Data != null)
                    {
                        textos.AddRange(jikanResponse.Data.Take(3).Select(r => r.ReviewText ?? ""));
                    }
                }
                else if (categoriaId == 2)
                {
                    var tmdbClient = _httpClientFactory.CreateClient("TmdbClient");
                    var tipo = tagsString.Contains("Série") ? "tv" : "movie";
                    var tmdbResponse = await tmdbClient.GetFromJsonAsync<TmdbReviewsResponse>($"{tipo}/{externalId}/reviews", stoppingToken);
                    if (tmdbResponse?.Results != null)
                    {
                        textos.AddRange(tmdbResponse.Results.Take(3).Select(r => r.Content ?? ""));
                    }
                }
            }
            catch { }
        }

        var textoLimpo = textos.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        return textoLimpo.Any() ? string.Join(" | ", textoLimpo) : string.Empty;
    }

    private async Task ProcessarJikanAsync(CancellationToken stoppingToken)
    {
        try
        {
            var jikanClient = _httpClientFactory.CreateClient("JikanClient");
            var apiClient = _httpClientFactory.CreateClient("ApiClient");
            var response = await jikanClient.GetFromJsonAsync<JikanResponse>("seasons/now", stoppingToken);

            if (response?.Data != null)
            {
                foreach (var anime in response.Data.Take(15))
                {
                    if (string.IsNullOrWhiteSpace(anime.Title)) continue;
                    var tags = anime.Genres?.Select(g => g.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
                    var imageUrl = anime.Images?.Jpg?.ImageUrl;
                    var dtoSync = new { Nome = anime.Title, CategoriaID = 1, Tags = tags, ExternalID = anime.MalId, Sinopse = anime.Synopsis, ImagemUrl = imageUrl };
                    var syncResponse = await apiClient.PostAsJsonAsync("/franquias/sync", dtoSync, stoppingToken);
                    if (syncResponse.IsSuccessStatusCode)
                    {
                        var syncResult = await syncResponse.Content.ReadFromJsonAsync<SyncResult>(cancellationToken: stoppingToken);
                        if (syncResult != null)
                        {
                            var notaBaseNormalizada = (decimal)((anime.Score ?? 0) * 10);
                            var textoComunidade = await BuscarTextosComunidadeAsync(anime.Title, 1, anime.MalId, string.Join(",", tags), stoppingToken);
                            var textoAnalise = !string.IsNullOrWhiteSpace(textoComunidade) ? textoComunidade : anime.Synopsis;
                            var sentimentoCalculado = await AnalisarSentimentoAsync(textoAnalise, stoppingToken);
                            var dto = new { FranquiaID = syncResult.FranquiaID, HypeScore = notaBaseNormalizada, VolumeMencoes = anime.Members, SentimentoPositivo = Math.Round(sentimentoCalculado, 2) };
                            await apiClient.PostAsJsonAsync("/monitoramento", dto, stoppingToken);
                            await AvaliarRegrasAsync(apiClient, dto.FranquiaID, anime.Title, dto.HypeScore, stoppingToken);
                        }
                    }
                }
            }
        }
        catch { }
    }

    private async Task ProcessarTmdbAsync(CancellationToken stoppingToken)
    {
        try
        {
            var tmdbClient = _httpClientFactory.CreateClient("TmdbClient");
            var apiClient = _httpClientFactory.CreateClient("ApiClient");
            var response = await tmdbClient.GetFromJsonAsync<TmdbResponse>("trending/all/day?language=pt-BR", stoppingToken);

            if (response?.Results != null)
            {
                foreach (var media in response.Results.Take(15))
                {
                    var title = !string.IsNullOrWhiteSpace(media.Title) ? media.Title : media.Name;
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    var tags = new List<string> { media.MediaType == "tv" ? "Série" : "Filme" };
                    var imageUrl = !string.IsNullOrEmpty(media.PosterPath) ? $"https://image.tmdb.org/t/p/w500{media.PosterPath}" : null;
                    var dtoSync = new { Nome = title, CategoriaID = 2, Tags = tags, ExternalID = media.Id, Sinopse = media.Overview, ImagemUrl = imageUrl };
                    var syncResponse = await apiClient.PostAsJsonAsync("/franquias/sync", dtoSync, stoppingToken);
                    if (syncResponse.IsSuccessStatusCode)
                    {
                        var syncResult = await syncResponse.Content.ReadFromJsonAsync<SyncResult>(cancellationToken: stoppingToken);
                        if (syncResult != null)
                        {
                            var notaBaseNormalizada = (decimal)(media.VoteAverage * 10);
                            var textoComunidade = await BuscarTextosComunidadeAsync(title, 2, media.Id, string.Join(",", tags), stoppingToken);
                            var textoAnalise = !string.IsNullOrWhiteSpace(textoComunidade) ? textoComunidade : media.Overview;
                            var sentimentoCalculado = await AnalisarSentimentoAsync(textoAnalise, stoppingToken);
                            var dto = new { FranquiaID = syncResult.FranquiaID, HypeScore = notaBaseNormalizada, VolumeMencoes = media.VoteCount, SentimentoPositivo = Math.Round(sentimentoCalculado, 2) };
                            await apiClient.PostAsJsonAsync("/monitoramento", dto, stoppingToken);
                            await AvaliarRegrasAsync(apiClient, dto.FranquiaID, title, dto.HypeScore, stoppingToken);
                        }
                    }
                }
            }
        }
        catch { }
    }

    private async Task AvaliarRegrasAsync(HttpClient apiClient, int franquiaId, string franquiaNome, decimal hypeScore, CancellationToken stoppingToken)
    {
        try
        {
            var fluxos = await apiClient.GetFromJsonAsync<List<Fluxo>>("/fluxos", stoppingToken);
            if (fluxos == null) return;

            foreach (var fluxo in fluxos)
            {
                var inicioNode = fluxo.Nodes.FirstOrDefault(n => n.Tipo == "inicio");
                if (inicioNode == null) continue;

                var currentNode = inicioNode;
                var hasMatchedCondition = false;

                while (currentNode != null)
                {
                    if (currentNode.Tipo == "condicao")
                    {
                        var match = Regex.Match(currentNode.Label, @"Hype\s*([<>])\s*(\d+)");
                        if (match.Success)
                        {
                            var operador = match.Groups[1].Value;
                            var valorAlvo = decimal.Parse(match.Groups[2].Value);
                            bool condicaoAtendida = operador == ">" ? hypeScore > valorAlvo : hypeScore < valorAlvo;
                            if (!condicaoAtendida) break;
                            hasMatchedCondition = true;
                        }
                    }

                    if ((currentNode.Tipo == "acao" || currentNode.Tipo == "fim") && hasMatchedCondition)
                    {
                        var alerta = new { FranquiaID = franquiaId, FluxoID = fluxo.FluxoID, Mensagem = $"Alerta disparado para {franquiaNome}: {currentNode.Label} (Hype Atual: {hypeScore:F1})" };
                        await apiClient.PostAsJsonAsync("/alertas", alerta, stoppingToken);
                        break;
                    }

                    var conn = fluxo.Connections.FirstOrDefault(c => c.SourceNodeID == currentNode.NodeID);
                    currentNode = conn != null ? fluxo.Nodes.FirstOrDefault(n => n.NodeID == conn.TargetNodeID) : null;
                }
            }
        }
        catch { }
    }
}

public class NlpResponse { [JsonPropertyName("score")] public double Score { get; set; } }
public class JikanResponse { [JsonPropertyName("data")] public List<JikanAnime>? Data { get; set; } }
public class JikanAnime { [JsonPropertyName("mal_id")] public int MalId { get; set; } [JsonPropertyName("title")] public string? Title { get; set; } [JsonPropertyName("score")] public double? Score { get; set; } [JsonPropertyName("members")] public int Members { get; set; } [JsonPropertyName("synopsis")] public string? Synopsis { get; set; } [JsonPropertyName("genres")] public List<JikanGenre>? Genres { get; set; } [JsonPropertyName("images")] public JikanImages? Images { get; set; } }
public class JikanGenre { [JsonPropertyName("name")] public string? Name { get; set; } }
public class JikanImages { [JsonPropertyName("jpg")] public JikanJpg? Jpg { get; set; } }
public class JikanJpg { [JsonPropertyName("image_url")] public string? ImageUrl { get; set; } }
public class TmdbResponse { [JsonPropertyName("results")] public List<TmdbMedia>? Results { get; set; } }
public class TmdbMedia { [JsonPropertyName("id")] public int Id { get; set; } [JsonPropertyName("title")] public string? Title { get; set; } [JsonPropertyName("name")] public string? Name { get; set; } [JsonPropertyName("vote_average")] public double VoteAverage { get; set; } [JsonPropertyName("vote_count")] public int VoteCount { get; set; } [JsonPropertyName("overview")] public string? Overview { get; set; } [JsonPropertyName("media_type")] public string? MediaType { get; set; } [JsonPropertyName("poster_path")] public string? PosterPath { get; set; } }
public class SyncResult { public int FranquiaID { get; set; } }
public class Fluxo { public int FluxoID { get; set; } public string Nome { get; set; } = string.Empty; public List<Node> Nodes { get; set; } = new(); public List<Connection> Connections { get; set; } = new(); }
public class Node { public string NodeID { get; set; } = string.Empty; public string Tipo { get; set; } = string.Empty; public string Label { get; set; } = string.Empty; }
public class Connection { public string SourceNodeID { get; set; } = string.Empty; public string TargetNodeID { get; set; } = string.Empty; }
public class RedditResponse { [JsonPropertyName("data")] public RedditData? Data { get; set; } }
public class RedditData { [JsonPropertyName("children")] public List<RedditChild>? Children { get; set; } }
public class RedditChild { [JsonPropertyName("data")] public RedditPostData? Data { get; set; } }
public class RedditPostData { [System.Text.Json.Serialization.JsonPropertyName("title")] public string? Title { get; set; } [System.Text.Json.Serialization.JsonPropertyName("selftext")] public string? Selftext { get; set; } }
public class JikanReviewsResponse { [JsonPropertyName("data")] public List<JikanReview>? Data { get; set; } }
public class JikanReview { [JsonPropertyName("review")] public string? ReviewText { get; set; } }
public class TmdbReviewsResponse { [JsonPropertyName("results")] public List<TmdbReview>? Results { get; set; } }
public class TmdbReview { [JsonPropertyName("content")] public string? Content { get; set; } }
public class YoutubeSearchResponse { [JsonPropertyName("items")] public List<YoutubeItem>? Items { get; set; } }
public class YoutubeItem { [JsonPropertyName("id")] public YoutubeId? Id { get; set; } [JsonPropertyName("snippet")] public YoutubeSnippet? Snippet { get; set; } }
public class YoutubeId { [JsonPropertyName("videoId")] public string? VideoId { get; set; } }
public class YoutubeSnippet { [JsonPropertyName("title")] public string? Title { get; set; } [JsonPropertyName("description")] public string? Description { get; set; } [JsonPropertyName("channelTitle")] public string? ChannelTitle { get; set; } }

public class AnilistThreadsResponse { [System.Text.Json.Serialization.JsonPropertyName("data")] public AnilistThreadsData? Data { get; set; } }
public class AnilistThreadsData { [System.Text.Json.Serialization.JsonPropertyName("Page")] public AnilistThreadsPage? Page { get; set; } }
public class AnilistThreadsPage { [System.Text.Json.Serialization.JsonPropertyName("threads")] public List<AnilistThreadItem>? Threads { get; set; } }
public class AnilistThreadItem { [System.Text.Json.Serialization.JsonPropertyName("title")] public string? Title { get; set; } [System.Text.Json.Serialization.JsonPropertyName("body")] public string? Body { get; set; } }
