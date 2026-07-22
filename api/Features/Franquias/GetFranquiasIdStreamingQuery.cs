using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Franquias;

public class GetFranquiasIdStreamingQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class GetFranquiasIdStreamingHandler : IRequestHandler<GetFranquiasIdStreamingQuery, IResult>
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public GetFranquiasIdStreamingHandler(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IResult> Handle(GetFranquiasIdStreamingQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var franquia = await connection.QuerySingleOrDefaultAsync<dynamic>(
        "SELECT ExternalID, CategoriaID, (SELECT STRING_AGG(t.Nome, ', ') FROM FranquiaTags ft INNER JOIN Tags t ON ft.TagID = t.TagID WHERE ft.FranquiaID = Franquias.FranquiaID) as TagsString FROM Franquias WHERE FranquiaID = @Id",
        new { Id = request.Id });

    if (franquia == null) return Results.Ok(new List<object>());

    var providers = new List<object>();

    try
    {
        if (franquia.ExternalID != null && franquia.CategoriaID == 2)
        {
            var tmdb = _httpClientFactory.CreateClient("TmdbClient");
            var tipo = (franquia.TagsString != null && ((string)franquia.TagsString).Contains("Série")) ? "tv" : "movie";
            var res = await tmdb.GetFromJsonAsync<TmdbWatchProvidersResponse>($"{tipo}/{franquia.ExternalID}/watch/providers");
            var br = res?.Results?.ContainsKey("BR") == true ? res.Results["BR"] : null;
            var all = (br?.Flatrate ?? new List<TmdbProviderItem>())
                .Concat(br?.Free ?? new List<TmdbProviderItem>())
                .Concat(br?.Buy ?? new List<TmdbProviderItem>())
                .GroupBy(p => p.ProviderId)
                .Select(g => g.First())
                .Select(p => new {
                    NomeProvider = p.ProviderName,
                    LogoUrl = $"https://image.tmdb.org/t/p/original{p.LogoPath}",
                    Tipo = br?.Flatrate?.Any(f => f.ProviderId == p.ProviderId) == true ? "Streaming" :
                           br?.Free?.Any(f => f.ProviderId == p.ProviderId) == true ? "Grátis" : "Compra"
                }).ToList<object>();

            providers.AddRange(all);
        }
        else if (franquia.ExternalID != null && (franquia.CategoriaID == 1 || franquia.CategoriaID == 3))
        {
            var tmdb = _httpClientFactory.CreateClient("TmdbClient");
            var searchRes = await tmdb.GetFromJsonAsync<TmdbSearchResponse>($"search/tv?query={Uri.EscapeDataString("anime")}&language=pt-BR");
            var dbProviders = await connection.QueryAsync("SELECT NomeProvider, LogoUrl, Tipo FROM StreamingProviders WHERE FranquiaID = @Id", new { Id = request.Id });
            providers.AddRange(dbProviders.Select(p => new { NomeProvider = (string)p.NomeProvider, LogoUrl = (string)(p.LogoUrl ?? ""), Tipo = (string)(p.Tipo ?? "Streaming") }).ToList<object>());

            if (!providers.Any())
            {
                providers.AddRange(new List<object>
                {
                    new { NomeProvider = "Crunchyroll", LogoUrl = "https://www.crunchyroll.com/build/assets/img/favicons/favicon-192x192.png", Tipo = "Streaming" },
                    new { NomeProvider = "Funimation", LogoUrl = "https://www.funimation.com/wp-content/uploads/2021/04/funimation-favicon.png", Tipo = "Streaming" }
                });
            }
        }
    }
    catch
    {
        var dbProviders = await connection.QueryAsync("SELECT NomeProvider, LogoUrl, Tipo FROM StreamingProviders WHERE FranquiaID = @Id", new { Id = request.Id });
        providers.AddRange(dbProviders.Select(p => new { NomeProvider = (string)p.NomeProvider, LogoUrl = (string)(p.LogoUrl ?? ""), Tipo = (string)(p.Tipo ?? "Streaming") }));
    }

    return Results.Ok(providers);

    }
}
