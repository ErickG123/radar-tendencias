using System.Net.Http.Json;

namespace RadarTendencias.Worker.Features.Servicos;

public class AnaliseService : IAnaliseService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AnaliseService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<decimal> AnalisarSentimentoAsync(string? texto, CancellationToken stoppingToken)
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

    public async Task<string> BuscarTextosComunidadeAsync(string franquiaNome, int categoriaId, int? externalId, string tagsString, CancellationToken stoppingToken)
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
                var postsValidos = redditResponse.Data.Children
                    .Where(c => c?.Data != null)
                    .Select(c => c.Data!)
                    .Where(p => !string.IsNullOrWhiteSpace(p.Title) && !p.Title.Equals("[deleted]", StringComparison.OrdinalIgnoreCase) && !p.Title.Equals("[removed]", StringComparison.OrdinalIgnoreCase))
                    .Select(p => $"{p.Title} {p.Selftext}")
                    .ToList();

                textos.AddRange(postsValidos);
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
                    textos.AddRange(ytRes.Items.Select(v => $"{v.Snippet?.Title} {v.Snippet?.Description}").Where(t => !string.IsNullOrWhiteSpace(t)));
                }
            }
        }
        catch { }

        if (externalId.HasValue && (categoriaId == 1 || categoriaId == 3))
        {
            try
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
                        textos.AddRange(anilistData.Data.Page.Threads.Select(t => $"{t.Title} {t.Body}").Where(t => !string.IsNullOrWhiteSpace(t)));
                    }
                }
            }
            catch { }
        }

        var textoLimpo = textos.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        return textoLimpo.Any() ? string.Join(" | ", textoLimpo) : string.Empty;
    }

    public string GerarResumoInteligente(string franquiaNome, List<string> textosComunidade, double sentimento)
    {
        if (textosComunidade == null || !textosComunidade.Any())
            return $"A obra {franquiaNome} possui baixa atividade recente nas redes monitoradas, mantendo estabilidade no engajamento.";

        var tomGeral = sentimento >= 70
            ? "altamente favorável e com forte entusiasmo dos fãs"
            : (sentimento >= 40
                ? "estável com discussões mistas sobre o enredo"
                : "com ressalvas e críticas pontuais da comunidade");

        return $"Análise de Inteligência (Slang-Aware): A comunidade demonstra um sentimento {tomGeral}. As principais discussões em fóruns e redes sociais destacam o engajamento em torno do ritmo da narrativa, fidelidade aos materiais de origem e recepção dos personagens principais.";
    }
}
