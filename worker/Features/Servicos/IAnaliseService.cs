namespace RadarTendencias.Worker.Features.Servicos;

public interface IAnaliseService
{
    Task<decimal> AnalisarSentimentoAsync(string? texto, CancellationToken stoppingToken);
    Task<string> BuscarTextosComunidadeAsync(string franquiaNome, int categoriaId, int? externalId, string tagsString, CancellationToken stoppingToken);
    string GerarResumoInteligente(string franquiaNome, List<string> textosComunidade, double sentimento);
}
