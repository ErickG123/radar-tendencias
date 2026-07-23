using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Calendario;

public class GetCalendarioSemanaQuery : IRequest<IResult>
{

}

public class GetCalendarioSemanaHandler : IRequestHandler<GetCalendarioSemanaQuery, IResult>
{


    private readonly IConfiguration _config;

    public GetCalendarioSemanaHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetCalendarioSemanaQuery request, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        var sql = @"
            SELECT 
                f.Nome as nome, 
                c.Nome as categoria, 
                p.DiaSemana as diaSemana, 
                p.HorarioEmissao as horario, 
                p.EpisodioAtual as episodio
            FROM ProgramacaoSemanal p
            INNER JOIN Franquias f ON p.FranquiaID = f.FranquiaID
            INNER JOIN Categorias c ON f.CategoriaID = c.CategoriaID
            ORDER BY p.DiaSemana ASC, p.HorarioEmissao ASC
        ";
        var resultados = await connection.QueryAsync<dynamic>(sql);
        return Results.Ok(resultados);
    }
}
