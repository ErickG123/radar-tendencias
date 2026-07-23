using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Monitoramento;

public class PostMonitoramentoQuery : IRequest<IResult>
{
    public MonitoramentoDTO Dto { get; set; }
}

public class PostMonitoramentoHandler : IRequestHandler<PostMonitoramentoQuery, IResult>
{
    private readonly IConfiguration _config;

    public PostMonitoramentoHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(PostMonitoramentoQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sql = @"INSERT INTO MonitoramentoHype (FranquiaID, HypeScore, VolumeMencoes, SentimentoPositivo, ResumoIA) 
                VALUES (@FranquiaID, @HypeScore, @VolumeMencoes, @SentimentoPositivo, @ResumoIA)";
    await connection.ExecuteAsync(sql, request.Dto);
    return Results.Created();

    }
}
