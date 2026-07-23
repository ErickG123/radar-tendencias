using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace RadarTendencias.Worker.Jobs;

// Opção 1: Serviço preparado para uso de API de terceiros (Rainforest API)
public class AmazonRainforestScraperJob
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AmazonRainforestScraperJob(IConfiguration config)
    {
        _httpClient = new HttpClient();
        _apiKey = config["RainforestApiKey"] ?? "SUA_API_KEY_AQUI"; 
    }

    public async Task<List<MangaRankingItem>> ExtrairRankingMangasAsync()
    {
        var resultados = new List<MangaRankingItem>();
        
        try
        {
            // Rainforest API para Amazon Bestsellers
            var url = $"https://api.rainforestapi.com/request?api_key={_apiKey}&type=bestsellers&url=https://www.amazon.com.br/gp/bestsellers/books/7872782011";
            var response = await _httpClient.GetFromJsonAsync<RainforestResponse>(url);

            if (response?.Bestsellers != null)
            {
                foreach (var item in response.Bestsellers)
                {
                    resultados.Add(new MangaRankingItem
                    {
                        Titulo = item.Title,
                        PosicaoRanking = item.Rank
                    });
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Falha na Rainforest API: " + ex.Message, ex);
        }

        return resultados;
    }

    private class RainforestResponse
    {
        public List<RainforestItem>? Bestsellers { get; set; }
    }

    private class RainforestItem
    {
        public int Rank { get; set; }
        public required string Title { get; set; }
    }
}
