using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Monitoramento;

public class PutAlertasIdLerQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class PutAlertasIdLerHandler : IRequestHandler<PutAlertasIdLerQuery, IResult>
{
    private readonly IConfiguration _config;

    public PutAlertasIdLerHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(PutAlertasIdLerQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sql = "UPDATE Alertas SET Lido = 1 WHERE AlertaID = @Id";
    await connection.ExecuteAsync(sql, new { Id = request.Id });
    return Results.NoContent();

    }
}
