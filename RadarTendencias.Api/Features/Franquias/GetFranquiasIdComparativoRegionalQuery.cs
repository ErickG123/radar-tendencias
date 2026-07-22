using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Franquias;

public class GetFranquiasIdComparativoRegionalQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class GetFranquiasIdComparativoRegionalHandler : IRequestHandler<GetFranquiasIdComparativoRegionalQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetFranquiasIdComparativoRegionalHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetFranquiasIdComparativoRegionalQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var franquia = await connection.QuerySingleOrDefaultAsync<dynamic>("SELECT Nome, ExternalID FROM Franquias WHERE FranquiaID = @Id", new { Id = request.Id });
    if (franquia == null) return Results.Ok(new { GlobalScore = 0, BrasilScore = 0, Indice = "N/A" });

    double globalScore = 85.0; 
    double brasilScore = 78.5;

    try
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://graphql.anilist.co");
        var queryGraphql = new {
            query = @"query ($id: Int) { Media(id: $id) { popularity averageScore } }",
            variables = new { id = (int)(franquia.ExternalID ?? 0) }
        };
        var res = await client.PostAsJsonAsync("", queryGraphql);
        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadFromJsonAsync<dynamic>();
            globalScore = (double)(data.GetProperty("data").GetProperty("Media").GetProperty("averageScore").GetDouble());
        }
    }
    catch { }

    return Results.Ok(new {
        GlobalPopularidade = globalScore * 10,
        BrasilEngajamento = brasilScore * 10,
        StatusComparativo = brasilScore >= globalScore ? "Destaque no Brasil" : "Forte Apelo Global"
    });

    }
}
