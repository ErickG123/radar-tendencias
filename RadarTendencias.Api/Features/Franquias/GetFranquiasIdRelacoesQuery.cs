using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Franquias;

public class GetFranquiasIdRelacoesQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class GetFranquiasIdRelacoesHandler : IRequestHandler<GetFranquiasIdRelacoesQuery, IResult>
{
    private readonly IConfiguration _config;

    public GetFranquiasIdRelacoesHandler(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Handle(GetFranquiasIdRelacoesQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var franquia = await connection.QuerySingleOrDefaultAsync<dynamic>("SELECT Nome, ExternalID FROM Franquias WHERE FranquiaID = @Id", new { Id = request.Id });
    if (franquia == null || franquia.ExternalID == null) return Results.Ok(new List<object>());

    using var client = new HttpClient();
    client.BaseAddress = new Uri("https://graphql.anilist.co");

    var queryGraphql = new {
        query = @"query ($id: Int) { Media(id: $id) { relations { edges { relationType node { id title { romaji english } type coverImage { large } } } } } }",
        variables = new { id = (int)franquia.ExternalID }
    };

    var response = await client.PostAsJsonAsync("", queryGraphql);
    if (!response.IsSuccessStatusCode) return Results.Ok(new List<object>());

    var result = await response.Content.ReadFromJsonAsync<AnilistRelationsResponse>();
    if (result?.Data?.Media?.Relations?.Edges == null) return Results.Ok(new List<object>());

    var lista = result.Data.Media.Relations.Edges
        .Where(e => e.Node != null)
        .Select(e => new {
            TipoRelacao = e.RelationType ?? "Relacionado",
            Nome = e.Node!.Title?.English ?? e.Node.Title?.Romaji ?? "Desconhecido",
            TipoMidia = e.Node.Type ?? "ANIME",
            ImagemUrl = e.Node.CoverImage?.Large
        }).ToList();

    return Results.Ok(lista);

    }
}
