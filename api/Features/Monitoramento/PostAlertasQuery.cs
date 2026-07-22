using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Monitoramento;

public class PostAlertasQuery : IRequest<IResult>
{
    public AlertaDTO Dto { get; set; }
}

public class PostAlertasHandler : IRequestHandler<PostAlertasQuery, IResult>
{
    private readonly IConfiguration _config;

    public PostAlertasHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(PostAlertasQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sql = "INSERT INTO Alertas (FranquiaID, FluxoID, Mensagem) VALUES (@FranquiaID, @FluxoID, @Mensagem)";
    await connection.ExecuteAsync(sql, request.Dto);
    return Results.Created();

    }
}
