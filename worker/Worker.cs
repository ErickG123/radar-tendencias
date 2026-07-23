using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Worker.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace RadarTendencias.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AmazonScraperJob _amazonScraper;
    private readonly IConfiguration _configuration;

    public Worker(IServiceProvider serviceProvider, AmazonScraperJob amazonScraper, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _amazonScraper = amazonScraper;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                var configQuery = "SELECT TOP 1 * FROM ConfiguracoesWorker WHERE Id = 1";
                var workerConfig = await connection.QuerySingleOrDefaultAsync<WorkerConfigDto>(configQuery);

                if (workerConfig == null || (!workerConfig.ScraperHabilitado && !workerConfig.ForcarExecucao))
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                var itensExtraidos = await _amazonScraper.ExtrairRankingMangasAsync();

                if (itensExtraidos.Any())
                {
                    foreach (var item in itensExtraidos)
                    {
                        var franquiaId = await connection.QuerySingleOrDefaultAsync<int?>(
                            "SELECT FranquiaID FROM Franquias WHERE Nome = @Nome", new { Nome = item.Titulo });

                        if (!franquiaId.HasValue)
                        {
                            franquiaId = await connection.QuerySingleAsync<int>(
                                "INSERT INTO Franquias (Nome, CategoriaID, Ativo, DataCriacao) OUTPUT INSERTED.FranquiaID VALUES (@Nome, 1, 1, GETDATE())",
                                new { Nome = item.Titulo });
                        }

                        // Converter posição BSR em Score invertido (quanto menor a posição, maior o HypeScore 1-100)
                        decimal hypeScore = item.PosicaoRanking > 0 && item.PosicaoRanking <= 100 
                            ? 100m - (item.PosicaoRanking - 1) 
                            : 10m;

                        await connection.ExecuteAsync(
                            "INSERT INTO MonitoramentoHype (FranquiaID, HypeScore, VolumeMencoes, SentimentoPositivo, DataMedicao) VALUES (@Id, @Score, 100, 1.0, GETDATE())",
                            new { Id = franquiaId.Value, Score = hypeScore });

                        // Atualiza VolumeProdutosAmazon de forma análoga à posição (apenas ilustrativo pro painel)
                        var volumeSimulado = (100 - item.PosicaoRanking) * 100;
                        if (volumeSimulado < 0) volumeSimulado = 0;
                            
                        if (await connection.ExecuteAsync("UPDATE ImpactoComercial SET VolumeProdutosAmazon = @Vol, DataAtualizacao = GETDATE() WHERE FranquiaID = @Id", new { Vol = volumeSimulado, Id = franquiaId.Value }) == 0)
                        {
                            await connection.ExecuteAsync(
                                "INSERT INTO ImpactoComercial (FranquiaID, VolumeProdutosAmazon, VolumeColecionaveis, PrecoMedio, DataAtualizacao) VALUES (@Id, @Vol, 0, 0, GETDATE())",
                                new { Id = franquiaId.Value, Vol = volumeSimulado });
                        }
                    }

                    var jsonDetalhes = JsonSerializer.Serialize(itensExtraidos);
                    await connection.ExecuteAsync(
                        "INSERT INTO WorkerLogs (DataExecucao, Status, ItensProcessados, MensagemErro, DetalhesJson) VALUES (@Data, 'Sucesso', @Qtd, 'Extração Playwright Amazon BSR.', @Detalhes)",
                        new { Data = DateTime.UtcNow, Qtd = itensExtraidos.Count, Detalhes = jsonDetalhes });
                }
                else
                {
                    await connection.ExecuteAsync(
                        "INSERT INTO WorkerLogs (DataExecucao, Status, ItensProcessados, MensagemErro) VALUES (@Data, 'Erro', 0, 'Nenhum item extraído da Amazon.')",
                        new { Data = DateTime.UtcNow });
                }
                
                if (workerConfig.ForcarExecucao)
                {
                    await connection.ExecuteAsync("UPDATE ConfiguracoesWorker SET ForcarExecucao = 0 WHERE Id = 1");
                }

                var endTime = DateTime.UtcNow.AddMinutes(workerConfig.IntervaloBaseMinutos);
                while (DateTime.UtcNow < endTime && !stoppingToken.IsCancellationRequested)
                {
                    var flag = await connection.QuerySingleOrDefaultAsync<bool>("SELECT ISNULL(ForcarExecucao, 0) FROM ConfiguracoesWorker WHERE Id = 1");
                    if (flag) break;
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                    await connection.ExecuteAsync(
                        "INSERT INTO WorkerLogs (DataExecucao, Status, ItensProcessados, MensagemErro) VALUES (@Data, 'Erro', 0, @Erro)",
                        new { Data = DateTime.UtcNow, Erro = ex.Message });
                }
                catch { }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    public class WorkerConfigDto
    {
        public bool ScraperHabilitado { get; set; }
        public int IntervaloBaseMinutos { get; set; }
        public bool ForcarExecucao { get; set; }
    }
}
