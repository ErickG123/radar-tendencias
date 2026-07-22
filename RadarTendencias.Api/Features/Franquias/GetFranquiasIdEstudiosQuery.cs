using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Franquias;

public class GetFranquiasIdEstudiosQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class GetFranquiasIdEstudiosHandler : IRequestHandler<GetFranquiasIdEstudiosQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetFranquiasIdEstudiosHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetFranquiasIdEstudiosQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var dados = await connection.QueryAsync("SELECT NomeEstudio FROM Estudios WHERE FranquiaID = @Id", new { Id = request.Id });
    return Results.Ok(dados);

    }
}
