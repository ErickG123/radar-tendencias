using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Notificacoes;

public class PatchNotificacoesIdLerQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class PatchNotificacoesIdLerHandler : IRequestHandler<PatchNotificacoesIdLerQuery, IResult>
{
    private readonly IConfiguration _config;

    public PatchNotificacoesIdLerHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(PatchNotificacoesIdLerQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    await connection.ExecuteAsync("UPDATE Notificacoes SET Lida = 1 WHERE NotificacaoID = @Id", new { Id = request.Id });
    return Results.Ok(new { Sucesso = true });

    }
}
