using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Favoritos;

public class GetFavoritosCheckFranquiaidQuery : IRequest<IResult>
{
    public int FranquiaId { get; set; }
}

public class GetFavoritosCheckFranquiaidHandler : IRequestHandler<GetFavoritosCheckFranquiaidQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetFavoritosCheckFranquiaidHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetFavoritosCheckFranquiaidQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sql = "SELECT COUNT(1) FROM Favoritos WHERE FranquiaID = @FranquiaID";
    var existe = await connection.ExecuteScalarAsync<int>(sql, new { FranquiaID = request.FranquiaId });
    return Results.Ok(new { Favorito = existe > 0 });

    }
}
