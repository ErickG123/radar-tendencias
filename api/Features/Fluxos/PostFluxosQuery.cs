using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Fluxos;

public class PostFluxosQuery : IRequest<IResult>
{
    public FluxoDTO Fluxo { get; set; }
}

public class PostFluxosHandler : IRequestHandler<PostFluxosQuery, IResult>
{
    private readonly IConfiguration _config;

    public PostFluxosHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(PostFluxosQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    await connection.OpenAsync();
    using var transaction = connection.BeginTransaction();
    try 
    {
        var sqlInsertFluxo = "INSERT INTO Fluxos (Nome) OUTPUT INSERTED.FluxoID VALUES (@Nome)";
        var fluxoId = await connection.QuerySingleAsync<int>(sqlInsertFluxo, new { request.Fluxo.Nome }, transaction);
        foreach (var node in request.Fluxo.Nodes)
        {
            var sqlInsertNode = "INSERT INTO FluxoNodes (NodeID, FluxoID, Tipo, Label, PosX, PosY) VALUES (@NodeID, @FluxoID, @Tipo, @Label, @PosX, @PosY)";
            await connection.ExecuteAsync(sqlInsertNode, new { node.NodeID, FluxoID = fluxoId, node.Tipo, node.Label, node.PosX, node.PosY }, transaction);
        }
        foreach (var conn in request.Fluxo.Connections)
        {
            var sqlInsertConn = "INSERT INTO FluxoConexoes (ConnectionID, FluxoID, SourceNodeID, SourcePortID, TargetNodeID, TargetPortID) VALUES (@ConnectionID, @FluxoID, @SourceNodeID, @SourcePortID, @TargetNodeID, @TargetPortID)";
            await connection.ExecuteAsync(sqlInsertConn, new { conn.ConnectionID, FluxoID = fluxoId, conn.SourceNodeID, conn.SourcePortID, conn.TargetNodeID, conn.TargetPortID }, transaction);
        }
        transaction.Commit();
        return Results.Created($"/fluxos/{fluxoId}", new { FluxoID = fluxoId });
    }
    catch
    {
        transaction.Rollback();
        throw;
    }

    }
}
