using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Favoritos;

public class PostFavoritosToggleFranquiaidQuery : IRequest<IResult>
{
    public int FranquiaId { get; set; }
}

public class PostFavoritosToggleFranquiaidHandler : IRequestHandler<PostFavoritosToggleFranquiaidQuery, IResult>
{
    private readonly IConfiguration _config;

    public PostFavoritosToggleFranquiaidHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(PostFavoritosToggleFranquiaidQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sqlCheck = "SELECT COUNT(1) FROM Favoritos WHERE FranquiaID = @FranquiaID";
    var existe = await connection.ExecuteScalarAsync<int>(sqlCheck, new { FranquiaID = request.FranquiaId });

    if (existe > 0)
    {
        await connection.ExecuteAsync("DELETE FROM Favoritos WHERE FranquiaID = @FranquiaID", new { FranquiaID = request.FranquiaId });
        return Results.Ok(new { Favorito = false, Mensagem = "Removido da Watchlist" });
    }
    else
    {
        await connection.ExecuteAsync("INSERT INTO Favoritos (FranquiaID) VALUES (@FranquiaID)", new { FranquiaID = request.FranquiaId });
        return Results.Ok(new { Favorito = true, Mensagem = "Adicionado à Watchlist" });
    }

    }
}
