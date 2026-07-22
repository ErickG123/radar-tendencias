using MediatR;

namespace RadarTendencias.Worker.Features.Comandos;

public class AvaliarRegrasCommand : IRequest
{
    public int FranquiaId { get; set; }
    public string FranquiaNome { get; set; } = string.Empty;
    public decimal HypeScore { get; set; }
}
