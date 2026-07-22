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


    public GetCalendarioSemanaHandler()
    {

    }

    public async Task<IResult> Handle(GetCalendarioSemanaQuery request, CancellationToken cancellationToken)
    {

    using var client = new HttpClient();
    client.BaseAddress = new Uri("https://graphql.anilist.co");
    
    var queryGraphql = new {
        query = @"query { Page(perPage: 20) { airingSchedules(sort: [TIME_ASC]) { airingAt episode media { id title { romaji english } coverImage { large } studios(isMain: true) { nodes { name } } } } } }"
    };

    var response = await client.PostAsJsonAsync("", queryGraphql);
    if (!response.IsSuccessStatusCode) return Results.Ok(new List<object>());

    var result = await response.Content.ReadFromJsonAsync<AnilistScheduleResponse>();
    if (result?.Data?.Page?.AiringSchedules == null) return Results.Ok(new List<object>());

    var lista = result.Data.Page.AiringSchedules.Select(s => new {
        DataHora = DateTimeOffset.FromUnixTimeSeconds(s.AiringAt).LocalDateTime,
        Episodio = s.Episode,
        Nome = s.Media?.Title?.English ?? s.Media?.Title?.Romaji ?? "Desconhecido",
        ImagemUrl = s.Media?.CoverImage?.Large,
        Estudio = s.Media?.Studios?.Nodes?.FirstOrDefault()?.Name ?? "Independente"
    }).ToList();

    return Results.Ok(lista);

    }
}
