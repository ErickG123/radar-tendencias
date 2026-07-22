using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Notificacoes;

public class GetNotificacoesQuery : IRequest<IResult>
{

}

public class GetNotificacoesHandler : IRequestHandler<GetNotificacoesQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetNotificacoesHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetNotificacoesQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sql = "SELECT NotificacaoID, FranquiaID, Titulo, Mensagem, Lida, DataCriacao FROM Notificacoes ORDER BY DataCriacao DESC";
    var dados = await connection.QueryAsync(sql);
    return Results.Ok(dados);

    }
}
