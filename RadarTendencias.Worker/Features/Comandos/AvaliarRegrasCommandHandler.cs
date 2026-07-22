using MediatR;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace RadarTendencias.Worker.Features.Comandos;

public class AvaliarRegrasCommandHandler : IRequestHandler<AvaliarRegrasCommand>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AvaliarRegrasCommandHandler(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task Handle(AvaliarRegrasCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var apiClient = _httpClientFactory.CreateClient("ApiClient");
            var fluxos = await apiClient.GetFromJsonAsync<List<Fluxo>>("/fluxos", cancellationToken);
            if (fluxos == null) return;

            foreach (var fluxo in fluxos)
            {
                var inicioNode = fluxo.Nodes.FirstOrDefault(n => n.Tipo == "inicio");
                if (inicioNode == null) continue;

                var currentNode = inicioNode;
                var hasMatchedCondition = false;

                while (currentNode != null)
                {
                    if (currentNode.Tipo == "condicao")
                    {
                        var match = Regex.Match(currentNode.Label, @"Hype\s*([<>])\s*(\d+)");
                        if (match.Success)
                        {
                            var operador = match.Groups[1].Value;
                            var valorAlvo = decimal.Parse(match.Groups[2].Value);
                            bool condicaoAtendida = operador == ">" ? request.HypeScore > valorAlvo : request.HypeScore < valorAlvo;
                            if (!condicaoAtendida) break;
                            hasMatchedCondition = true;
                        }
                    }

                    if ((currentNode.Tipo == "acao" || currentNode.Tipo == "fim") && hasMatchedCondition)
                    {
                        var alerta = new { FranquiaID = request.FranquiaId, FluxoID = fluxo.FluxoID, Mensagem = $"Alerta disparado para {request.FranquiaNome}: {currentNode.Label} (Hype Atual: {request.HypeScore:F1})" };
                        await apiClient.PostAsJsonAsync("/alertas", alerta, cancellationToken);
                        break;
                    }

                    var conn = fluxo.Connections.FirstOrDefault(c => c.SourceNodeID == currentNode.NodeID);
                    currentNode = conn != null ? fluxo.Nodes.FirstOrDefault(n => n.NodeID == conn.TargetNodeID) : null;
                }
            }
        }
        catch { }
    }
}
