using MediatR;
using RadarTendencias.Worker.Features.Comandos;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Net.Http.Json;

namespace RadarTendencias.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public Worker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int delayMinutes = 240; 
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                
                var configQuery = "SELECT TOP 1 * FROM ConfiguracoesWorker ORDER BY ConfiguracaoID DESC";
                var workerConfig = await connection.QuerySingleOrDefaultAsync<WorkerConfigDto>(configQuery);

                if (workerConfig != null)
                {
                    if (!workerConfig.AmazonScraperAtivo)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    delayMinutes = workerConfig.ModoPromocaoAtivo ? workerConfig.IntervaloPromocaoMinutos : workerConfig.IntervaloBaseMinutos;
                }

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new ProcessarJikanCommand(), stoppingToken);
                await mediator.Send(new ProcessarTmdbCommand(), stoppingToken);
            }
            catch
            {
                delayMinutes = 60;
            }

            await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
        }
    }

    private async Task NotificarApiAsync(int franquiaId, string nomeFranquia, string tipoAtualizacao)
    {
        try
        {
            using var httpClient = new HttpClient();
            var payload = new { FranquiaId = franquiaId, NomeFranquia = nomeFranquia, TipoAtualizacao = tipoAtualizacao };
            await httpClient.PostAsJsonAsync("http://localhost:8080/api/webhooks/worker-sync", payload);
        }
        catch { }
    }

    public class WorkerConfigDto
    {
        public bool AmazonScraperAtivo { get; set; }
        public int IntervaloBaseMinutos { get; set; }
        public bool ModoPromocaoAtivo { get; set; }
        public int IntervaloPromocaoMinutos { get; set; }
    }
}
