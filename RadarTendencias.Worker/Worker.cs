using MediatR;
using RadarTendencias.Worker.Features.Comandos;

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
        using (var scope = _serviceProvider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ProcessarJikanCommand(), stoppingToken);
            await mediator.Send(new ProcessarTmdbCommand(), stoppingToken);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new ProcessarJikanCommand(), stoppingToken);
                await mediator.Send(new ProcessarTmdbCommand(), stoppingToken);
            }
        }
    }
}
