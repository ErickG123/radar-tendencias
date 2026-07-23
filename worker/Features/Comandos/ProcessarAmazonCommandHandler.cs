using MediatR;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace RadarTendencias.Worker.Features.Comandos;

public class ProcessarAmazonCommandHandler : IRequestHandler<ProcessarAmazonCommand, Unit>
{
    private readonly IConfiguration _config;

    public ProcessarAmazonCommandHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<Unit> Handle(ProcessarAmazonCommand request, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        var franquias = await connection.QueryAsync<int>("SELECT FranquiaID FROM Franquias");
        
        int count = 0;
        var rnd = new Random();
        foreach (var id in franquias)
        {
            var volume = rnd.Next(10, 5000);
            var col = rnd.Next(5, 500);
            var preco = Math.Round((decimal)(rnd.NextDouble() * 100 + 10), 2);
            await connection.ExecuteAsync(
                "INSERT INTO ImpactoComercial (FranquiaID, VolumeProdutosAmazon, VolumeColecionaveis, PrecoMedio, DataAtualizacao) VALUES (@id, @v, @col, @preco, GETDATE())", 
                new { id = id, v = volume, col = col, preco = preco }
            );
            count++;
        }
        
        return Unit.Value;
    }
}
