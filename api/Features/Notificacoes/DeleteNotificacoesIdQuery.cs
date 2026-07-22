using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Notificacoes;

public class DeleteNotificacoesIdQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class DeleteNotificacoesIdHandler : IRequestHandler<DeleteNotificacoesIdQuery, IResult>
{
    private readonly IConfiguration _config;

    public DeleteNotificacoesIdHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(DeleteNotificacoesIdQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    await connection.ExecuteAsync("DELETE FROM Notificacoes WHERE NotificacaoID = @Id", new { Id = request.Id });
    return Results.Ok(new { Sucesso = true });

    }
}
