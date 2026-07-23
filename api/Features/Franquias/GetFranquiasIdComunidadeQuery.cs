using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using RadarTendencias.Api.Features.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Api.Features.Franquias;

public class GetFranquiasIdComunidadeQuery : IRequest<IResult>
{
    public int Id { get; set; }
}

public class GetFranquiasIdComunidadeHandler : IRequestHandler<GetFranquiasIdComunidadeQuery, IResult>
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public GetFranquiasIdComunidadeHandler(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IResult> Handle(GetFranquiasIdComunidadeQuery request, CancellationToken cancellationToken)
    {

    using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
    var sqlFranquia = @"SELECT Nome, CategoriaID, ExternalID, (SELECT STRING_AGG(t.Nome, ', ') FROM FranquiaTags ft INNER JOIN Tags t ON ft.TagID = t.TagID WHERE ft.FranquiaID = Franquias.FranquiaID) as TagsString FROM Franquias WHERE FranquiaID = @Id";
    var franquia = await connection.QuerySingleOrDefaultAsync<FranquiaDbResult>(sqlFranquia, new { Id = request.Id });
    
    if (franquia == null) return Results.Ok(new List<object>());

    var feedbacks = new List<ComunidadeFeedbackDTO>();

    try
    {
        var redditClient = _httpClientFactory.CreateClient("RedditClient");
        var query = Uri.EscapeDataString(franquia.Nome);
        var redditResponse = await redditClient.GetFromJsonAsync<RedditResponse>($"search.json?q={query}&sort=hot&limit=3");
        if (redditResponse?.Data?.Children != null)
        {
            feedbacks.AddRange(redditResponse.Data.Children.Select(c => new ComunidadeFeedbackDTO { Fonte = "Reddit", Autor = c.Data?.Author ?? "Usuário", Titulo = c.Data?.Title ?? "", Texto = c.Data?.Selftext ?? "", Url = $"https://reddit.com{c.Data?.Permalink}" }).Where(p => !string.IsNullOrWhiteSpace(p.Titulo)));
        }
    }
    catch { }

    try
    {
        using var ytClient = new HttpClient();
        var ytKey = _config["YouTubeApiKey"];
        var ytQuery = Uri.EscapeDataString(franquia.Nome + " review trailer");
        var ytRes = await ytClient.GetFromJsonAsync<YoutubeSearchResponse>($"https://www.googleapis.com/youtube/v3/search?part=snippet&q={ytQuery}&type=video&key={ytKey}&maxResults=2");
        if (ytRes?.Items != null)
        {
            feedbacks.AddRange(ytRes.Items.Select(v => new ComunidadeFeedbackDTO { Fonte = "YouTube", Autor = v.Snippet?.ChannelTitle ?? "Canal", Titulo = v.Snippet?.Title ?? "", Texto = v.Snippet?.Description ?? "", Url = $"https://youtube.com/watch?v={v.Id?.VideoId}" }));
        }
    }
    catch { }

    if (franquia.ExternalID != null)
    {
        try
        {
            if (franquia.CategoriaID == 1 || franquia.CategoriaID == 3)
            {
                var jikanClient = _httpClientFactory.CreateClient("JikanClient");
                var tipo = franquia.CategoriaID == 1 ? "anime" : "manga";
                var jikanResponse = await jikanClient.GetFromJsonAsync<JikanReviewsResponse>($"{tipo}/{franquia.ExternalID}/reviews");
                if (jikanResponse?.Data != null)
                {
                    feedbacks.AddRange(jikanResponse.Data.Take(2).Select(r => new ComunidadeFeedbackDTO { Fonte = "MyAnimeList", Autor = r.User?.Username ?? "Reviewer", Titulo = "Avaliação da Comunidade", Texto = r.ReviewText ?? "", Url = r.Url ?? "" }));
                }
            }
            else if (franquia.CategoriaID == 2)
            {
                var tmdbClient = _httpClientFactory.CreateClient("TmdbClient");
                var tipo = (franquia.TagsString != null && franquia.TagsString.Contains("Série")) ? "tv" : "movie";
                var tmdbResponse = await tmdbClient.GetFromJsonAsync<TmdbReviewsResponse>($"{tipo}/{franquia.ExternalID}/reviews");
                if (tmdbResponse?.Results != null)
                {
                    feedbacks.AddRange(tmdbResponse.Results.Take(2).Select(r => new ComunidadeFeedbackDTO { Fonte = "TMDB", Autor = r.Author ?? "Crítico", Titulo = "Review TMDB", Texto = r.Content ?? "", Url = r.Url ?? "" }));
                }
            }
        }
        catch { }
    }

    return Results.Ok(feedbacks.OrderBy(x => Guid.NewGuid()).ToList());

    }
}
