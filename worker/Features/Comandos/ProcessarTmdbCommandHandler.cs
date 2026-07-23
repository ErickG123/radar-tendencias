using MediatR;
using System.Net.Http.Json;
using RadarTendencias.Worker.Features.Servicos;

namespace RadarTendencias.Worker.Features.Comandos;

public class ProcessarTmdbCommandHandler : IRequestHandler<ProcessarTmdbCommand>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAnaliseService _analiseService;
    private readonly IMediator _mediator;

    public ProcessarTmdbCommandHandler(IHttpClientFactory httpClientFactory, IAnaliseService analiseService, IMediator mediator)
    {
        _httpClientFactory = httpClientFactory;
        _analiseService = analiseService;
        _mediator = mediator;
    }

    public async Task Handle(ProcessarTmdbCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tmdbClient = _httpClientFactory.CreateClient("TmdbClient");
            var apiClient = _httpClientFactory.CreateClient("ApiClient");
            var response = await tmdbClient.GetFromJsonAsync<TmdbResponse>("trending/all/day?language=pt-BR", cancellationToken);

            if (response?.Results != null)
            {
                foreach (var media in response.Results.Take(15))
                {
                    var title = !string.IsNullOrWhiteSpace(media.Title) ? media.Title : media.Name;
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    var tags = new List<string> { media.MediaType == "tv" ? "Série" : "Filme" };
                    var imageUrl = !string.IsNullOrEmpty(media.PosterPath) ? $"https://image.tmdb.org/t/p/w500{media.PosterPath}" : null;
                    var dtoSync = new { Nome = title, CategoriaID = 2, Tags = tags, ExternalID = media.Id, Sinopse = media.Overview, ImagemUrl = imageUrl };
                    var syncResponse = await apiClient.PostAsJsonAsync("/franquias/sync", dtoSync, cancellationToken);
                    if (syncResponse.IsSuccessStatusCode)
                    {
                        var syncResult = await syncResponse.Content.ReadFromJsonAsync<SyncResult>(cancellationToken: cancellationToken);
                        if (syncResult != null)
                        {
                            var notaBaseNormalizada = (decimal)(media.VoteAverage * 10);
                            var textoComunidade = await _analiseService.BuscarTextosComunidadeAsync(title, 2, media.Id, string.Join(",", tags), cancellationToken);
                            var textosLista = textoComunidade.Split(" | ", StringSplitOptions.RemoveEmptyEntries).ToList();
                            var textoAnalise = !string.IsNullOrWhiteSpace(textoComunidade) ? textoComunidade : media.Overview;
                            var sentimentoCalculado = await _analiseService.AnalisarSentimentoAsync(textoAnalise, cancellationToken);
                            var resumoIA = _analiseService.GerarResumoInteligente(title, textosLista, (double)sentimentoCalculado);
                            var dto = new { FranquiaID = syncResult.FranquiaID, HypeScore = notaBaseNormalizada, VolumeMencoes = media.VoteCount, SentimentoPositivo = Math.Round(sentimentoCalculado, 2), ResumoIA = resumoIA };
                            await apiClient.PostAsJsonAsync("/monitoramento", dto, cancellationToken);
                            
                            await _mediator.Send(new AvaliarRegrasCommand { 
                                FranquiaId = dto.FranquiaID, 
                                FranquiaNome = title, 
                                HypeScore = dto.HypeScore 
                            }, cancellationToken);
                        }
                    }
                }
            }
        }
        catch { }
    }
}
