using System.Text.Json;
using Microsoft.Data.SqlClient;
using Dapper;

namespace RadarTendencias.Api.Features.Franquias;

public static class FranquiasEndpoints
{
    public static void MapFranquiasEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/franquias/{id}/gerar-resumo", async (int id, IConfiguration config, IHttpClientFactory httpClientFactory) =>
        {
            using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
            
            var sqlFranquia = "SELECT Nome FROM Franquias WHERE FranquiaID = @Id";
            var nomeFranquia = await connection.QuerySingleOrDefaultAsync<string>(sqlFranquia, new { Id = id });
            if (nomeFranquia == null) return Results.NotFound();

            var sqlMonitoramento = "SELECT TOP 1 HypeScore, SentimentoPositivo, NuvemPalavras FROM MonitoramentoHype WHERE FranquiaID = @Id ORDER BY DataMedicao DESC";
            var monitoramento = await connection.QuerySingleOrDefaultAsync<dynamic>(sqlMonitoramento, new { Id = id });
            
            if (monitoramento == null) return Results.BadRequest();

            var palavrasList = new List<string>();
            if (!string.IsNullOrEmpty(monitoramento.NuvemPalavras))
            {
                var jsonPalavras = JsonDocument.Parse((string)monitoramento.NuvemPalavras);
                foreach (var element in jsonPalavras.RootElement.EnumerateArray())
                {
                    palavrasList.Add(element.GetProperty("word").GetString() ?? string.Empty);
                }
            }

            var requestBody = new 
            {
                franquia = nomeFranquia,
                hype = monitoramento.HypeScore,
                sentimento = monitoramento.SentimentoPositivo,
                palavras = palavrasList
            };

            var client = httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync("http://llmservice:5001/generate-summary", requestBody);
            
            if (!response.IsSuccessStatusCode) return Results.StatusCode(500);

            var llmResult = await response.Content.ReadFromJsonAsync<LlmResponse>();
            if (llmResult == null || string.IsNullOrEmpty(llmResult.Resumo)) return Results.StatusCode(500);
            
            var sqlUpdate = "UPDATE MonitoramentoHype SET ResumoIA = @Resumo WHERE FranquiaID = @Id AND DataMedicao = (SELECT MAX(DataMedicao) FROM MonitoramentoHype WHERE FranquiaID = @Id)";
            await connection.ExecuteAsync(sqlUpdate, new { Resumo = llmResult.Resumo, Id = id });

            return Results.Ok(new { Resumo = llmResult.Resumo });
        });

        app.MapGet("/api/franquias/ranking", async (IConfiguration config) =>
        {
            using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
            var sql = @"
                SELECT 
                    f.FranquiaID as franquiaId, 
                    f.Nome as nome, 
                    c.Nome as categoria, 
                    m.HypeScore as hypeScore, 
                    m.SentimentoPositivo as sentimentoPositivo,
                    ISNULL(i.VolumeProdutosAmazon, 0) as bsrPosition
                FROM Franquias f
                INNER JOIN Categorias c ON f.CategoriaID = c.CategoriaID
                LEFT JOIN (
                    SELECT FranquiaID, HypeScore, SentimentoPositivo,
                           ROW_NUMBER() OVER(PARTITION BY FranquiaID ORDER BY DataMedicao DESC) as rn
                    FROM MonitoramentoHype
                ) m ON f.FranquiaID = m.FranquiaID AND m.rn = 1
                LEFT JOIN (
                    SELECT FranquiaID, VolumeProdutosAmazon,
                           ROW_NUMBER() OVER(PARTITION BY FranquiaID ORDER BY DataAtualizacao DESC) as rn
                    FROM ImpactoComercial
                ) i ON f.FranquiaID = i.FranquiaID AND i.rn = 1
                ORDER BY i.VolumeProdutosAmazon DESC
            ";
            var ranking = await connection.QueryAsync<dynamic>(sql);
            return Results.Ok(ranking);
        });
    }
}

public class LlmResponse 
{ 
    public string Resumo { get; set; } = string.Empty; 
}
