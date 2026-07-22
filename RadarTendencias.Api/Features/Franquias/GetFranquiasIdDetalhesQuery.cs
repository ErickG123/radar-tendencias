using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Franquias;

public class GetFranquiasIdDetalhesQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class GetFranquiasIdDetalhesHandler : IRequestHandler<GetFranquiasIdDetalhesQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetFranquiasIdDetalhesHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetFranquiasIdDetalhesQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sqlFranquia = @"
        SELECT f.*, 
               (SELECT STRING_AGG(t.Nome, ', ') FROM FranquiaTags ft INNER JOIN Tags t ON ft.TagID = t.TagID WHERE ft.FranquiaID = f.FranquiaID) as TagsString
        FROM Franquias f WHERE f.FranquiaID = @Id";
    
    var franquia = await connection.QuerySingleOrDefaultAsync(sqlFranquia, new { Id = request.Id });
    if (franquia == null) return Results.NotFound();

    var sqlHistorico = "SELECT TOP 30 HypeScore, VolumeMencoes, SentimentoPositivo, DataMedicao FROM MonitoramentoHype WHERE FranquiaID = @Id ORDER BY DataMedicao DESC";
    var historico = await connection.QueryAsync(sqlHistorico, new { Id = request.Id });

    var sqlResumo = "SELECT TOP 1 ResumoIA FROM MonitoramentoHype WHERE FranquiaID = @Id ORDER BY DataMedicao DESC";
    var resumoIA = await connection.QuerySingleOrDefaultAsync<string>(sqlResumo, new { Id = request.Id });

    return Results.Ok(new { Detalhes = franquia, Historico = historico, ResumoIA = resumoIA ?? "O sistema ainda está processando os dados de menções recentes para gerar um resumo executivo desta obra." });

    }
}
