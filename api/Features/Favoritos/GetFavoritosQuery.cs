using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Favoritos;

public class GetFavoritosQuery : IRequest<IResult>
{

}

public class GetFavoritosHandler : IRequestHandler<GetFavoritosQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetFavoritosHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetFavoritosQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sql = @"
        SELECT f.FranquiaID, f.Nome, f.CategoriaID, f.ImagemUrl, fav.DataAdicao, m.HypeScore, m.SentimentoPositivo,
               (SELECT STRING_AGG(t.Nome, ', ') FROM FranquiaTags ft INNER JOIN Tags t ON ft.TagID = t.TagID WHERE ft.FranquiaID = f.FranquiaID) as TagsString
        FROM Favoritos fav
        INNER JOIN Franquias f ON fav.FranquiaID = f.FranquiaID
        LEFT JOIN (
            SELECT FranquiaID, HypeScore, SentimentoPositivo,
                   ROW_NUMBER() OVER(PARTITION BY FranquiaID ORDER BY DataMedicao DESC) as rn
            FROM MonitoramentoHype
        ) m ON m.FranquiaID = f.FranquiaID AND m.rn = 1
        ORDER BY fav.DataAdicao DESC";
    
    var favoritos = await connection.QueryAsync(sql);
    return Results.Ok(favoritos);

    }
}
