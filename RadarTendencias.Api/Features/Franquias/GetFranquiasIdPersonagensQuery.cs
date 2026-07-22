using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Franquias;

public class GetFranquiasIdPersonagensQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class GetFranquiasIdPersonagensHandler : IRequestHandler<GetFranquiasIdPersonagensQuery, IResult>
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public GetFranquiasIdPersonagensHandler(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IResult> Handle(GetFranquiasIdPersonagensQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sqlFranquia = @"SELECT ExternalID, CategoriaID, (SELECT STRING_AGG(t.Nome, ', ') FROM FranquiaTags ft INNER JOIN Tags t ON ft.TagID = t.TagID WHERE ft.FranquiaID = Franquias.FranquiaID) as TagsString FROM Franquias WHERE FranquiaID = @Id";
    var franquia = await connection.QuerySingleOrDefaultAsync(sqlFranquia, new { Id = request.Id });
    
    if (franquia == null || franquia.ExternalID == null) return Results.Ok(new List<PersonagemDTO>());

    var personagens = new List<PersonagemDTO>();

    try 
    {
        if (franquia.CategoriaID == 1 || franquia.CategoriaID == 3)
        {
            var jikan = _httpClientFactory.CreateClient("JikanClient");
            var tipo = franquia.CategoriaID == 1 ? "anime" : "manga";
            var response = await jikan.GetFromJsonAsync<JikanCharactersResponse>($"{tipo}/{franquia.ExternalID}/characters");
            if (response?.Data != null)
            {
                personagens = response.Data
                    .Where(c => c.Character?.Images?.Jpg?.ImageUrl != null && !c.Character.Images.Jpg.ImageUrl.Contains("questionmark"))
                    .Take(12)
                    .Select(c => new PersonagemDTO { Nome = c.Character?.Name ?? "", Papel = c.Role ?? "Personagem", ImagemUrl = c.Character?.Images?.Jpg?.ImageUrl ?? "" }).ToList();
            }
        }
        else if (franquia.CategoriaID == 2)
        {
            var tmdb = _httpClientFactory.CreateClient("TmdbClient");
            var tipo = (franquia.TagsString != null && franquia.TagsString.Contains("Série")) ? "tv" : "movie";
            var response = await tmdb.GetFromJsonAsync<TmdbCreditsResponse>($"{tipo}/{franquia.ExternalID}/credits?language=pt-BR");
            if (response?.Cast != null)
            {
                personagens = response.Cast
                    .Where(c => !string.IsNullOrEmpty(c.ProfilePath))
                    .Take(12)
                    .Select(c => new PersonagemDTO { Nome = c.Character ?? c.Name ?? "", Papel = "Ator/Personagem", ImagemUrl = $"https://image.tmdb.org/t/p/w200{c.ProfilePath}" }).ToList();
            }
        }
    } 
    catch { }

    return Results.Ok(personagens);

    }
}
