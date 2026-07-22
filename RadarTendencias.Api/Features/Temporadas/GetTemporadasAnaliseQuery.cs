using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Temporadas;

public class GetTemporadasAnaliseQuery : IRequest<IResult>
{
    public int Ano { get; set; }
    public string Temporada { get; set; }
}

public class GetTemporadasAnaliseHandler : IRequestHandler<GetTemporadasAnaliseQuery, IResult>
{


    public GetTemporadasAnaliseHandler()
    {

    }

    public async Task<IResult> Handle(GetTemporadasAnaliseQuery request, CancellationToken cancellationToken)
    {

    using var client = new HttpClient();
    client.BaseAddress = new Uri("https://graphql.anilist.co");

    var queryGraphql = new {
        query = @"query ($season: MediaSeason, $year: Int) { Page(perPage: 50) { media(season: $season, seasonYear: $year, type: ANIME, sort: [POPULARITY_DESC]) { id title { romaji english } coverImage { large } popularity averageScore trending genres } } }",
        variables = new { season = request.Temporada.ToUpper(), year = request.Ano }
    };

    var response = await client.PostAsJsonAsync("", queryGraphql);
    if (!response.IsSuccessStatusCode) return Results.Ok(new List<object>());

    var result = await response.Content.ReadFromJsonAsync<AnilistSeasonResponse>();
    if (result?.Data?.Page?.Media == null) return Results.Ok(new List<object>());

    var mediana = result.Data.Page.Media.Where(m => m.Popularity > 0).Select(m => m.Popularity).DefaultIfEmpty(0).Average();

    var lista = result.Data.Page.Media.Select(m => new {
        Id = m.Id,
        Nome = m.Title?.English ?? m.Title?.Romaji ?? "Desconhecido",
        ImagemUrl = m.CoverImage?.Large,
        Popularidade = m.Popularity,
        MediaScore = m.AverageScore ?? 0,
        Trending = m.Trending,
        Generos = m.Genres ?? new List<string>(),
        EhBlockbuster = m.Popularity >= mediana
    }).ToList();

    return Results.Ok(lista);

    }
}
