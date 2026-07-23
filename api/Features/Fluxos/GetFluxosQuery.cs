using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Fluxos;

public class GetFluxosQuery : IRequest<IResult>
{

}

public class GetFluxosHandler : IRequestHandler<GetFluxosQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetFluxosHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetFluxosQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var fluxos = await connection.QueryAsync<FluxoDTO>("SELECT * FROM Fluxos");
    foreach(var fluxo in fluxos)
    {
        fluxo.Nodes = (await connection.QueryAsync<NodeDTO>("SELECT * FROM FluxoNodes WHERE FluxoID = @Id", new { Id = fluxo.FluxoID })).ToList();
        fluxo.Connections = (await connection.QueryAsync<ConnectionDTO>("SELECT * FROM FluxoConexoes WHERE FluxoID = @Id", new { Id = fluxo.FluxoID })).ToList();
    }
    return Results.Ok(fluxos);

    }
}
