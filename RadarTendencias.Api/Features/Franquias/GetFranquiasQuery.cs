using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Franquias;

public class GetFranquiasQuery : IRequest<IResult>
{

}

public class GetFranquiasHandler : IRequestHandler<GetFranquiasQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetFranquiasHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetFranquiasQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var franquias = await connection.QueryAsync("SELECT * FROM Franquias");
    return Results.Ok(franquias);

    }
}
