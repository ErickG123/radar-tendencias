using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Monitoramento;

public class GetAlertasQuery : IRequest<IResult>
{

}

public class GetAlertasHandler : IRequestHandler<GetAlertasQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetAlertasHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetAlertasQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sql = @"SELECT a.*, f.Nome as FranquiaNome FROM Alertas a 
                INNER JOIN Franquias f ON a.FranquiaID = f.FranquiaID 
                ORDER BY a.DataAlerta DESC";
    var alertas = await connection.QueryAsync(sql);
    return Results.Ok(alertas);

    }
}
