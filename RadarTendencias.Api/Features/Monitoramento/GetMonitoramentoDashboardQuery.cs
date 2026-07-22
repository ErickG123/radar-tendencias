using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Monitoramento;

public class GetMonitoramentoDashboardQuery : IRequest<IResult>
{

}

public class GetMonitoramentoDashboardHandler : IRequestHandler<GetMonitoramentoDashboardQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetMonitoramentoDashboardHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetMonitoramentoDashboardQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
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

    }
}
