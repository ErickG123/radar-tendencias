using MediatR;
using System.Net.Http.Json;
using RadarTendencias.Worker.Features.Servicos;

namespace RadarTendencias.Worker.Features.Comandos;

public class ProcessarJikanCommandHandler : IRequestHandler<ProcessarJikanCommand>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAnaliseService _analiseService;
    private readonly IMediator _mediator;

    public ProcessarJikanCommandHandler(IHttpClientFactory httpClientFactory, IAnaliseService analiseService, IMediator mediator)
    {
        _httpClientFactory = httpClientFactory;
        _analiseService = analiseService;
        _mediator = mediator;
    }

    public async Task Handle(ProcessarJikanCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var jikanClient = _httpClientFactory.CreateClient("JikanClient");
            var apiClient = _httpClientFactory.CreateClient("ApiClient");
            var response = await jikanClient.GetFromJsonAsync<JikanResponse>("seasons/now", cancellationToken);

            if (response?.Data != null)
            {
                foreach (var anime in response.Data.Take(15))
                {
                    if (string.IsNullOrWhiteSpace(anime.Title)) continue;
                    var tags = anime.Genres?.Select(g => g.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
                    var imageUrl = anime.Images?.Jpg?.ImageUrl;
                    var dtoSync = new { Nome = anime.Title, CategoriaID = 1, Tags = tags, ExternalID = anime.MalId, Sinopse = anime.Synopsis, ImagemUrl = imageUrl };
                    var syncResponse = await apiClient.PostAsJsonAsync("/franquias/sync", dtoSync, cancellationToken);
                    if (syncResponse.IsSuccessStatusCode)
                    {
                        var syncResult = await syncResponse.Content.ReadFromJsonAsync<SyncResult>(cancellationToken: cancellationToken);
                        if (syncResult != null)
                        {
                            var notaBaseNormalizada = (decimal)((anime.Score ?? 0) * 10);
                            var textoComunidade = await _analiseService.BuscarTextosComunidadeAsync(anime.Title, 1, anime.MalId, string.Join(",", tags), cancellationToken);
                            var textosLista = textoComunidade.Split(" | ", StringSplitOptions.RemoveEmptyEntries).ToList();
                            var textoAnalise = !string.IsNullOrWhiteSpace(textoComunidade) ? textoComunidade : anime.Synopsis;
                            var sentimentoCalculado = await _analiseService.AnalisarSentimentoAsync(textoAnalise, cancellationToken);
                            var resumoIA = _analiseService.GerarResumoInteligente(anime.Title, textosLista, (double)sentimentoCalculado);
                            var dto = new { FranquiaID = syncResult.FranquiaID, HypeScore = notaBaseNormalizada, VolumeMencoes = anime.Members, SentimentoPositivo = Math.Round(sentimentoCalculado, 2), ResumoIA = resumoIA };
                            await apiClient.PostAsJsonAsync("/monitoramento", dto, cancellationToken);
                            
                            await _mediator.Send(new AvaliarRegrasCommand { 
                                FranquiaId = dto.FranquiaID, 
                                FranquiaNome = anime.Title, 
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
