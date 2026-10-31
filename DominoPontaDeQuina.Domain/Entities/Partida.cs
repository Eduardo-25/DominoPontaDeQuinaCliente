namespace DominoPontaDeQuina.Domain.Entities;

public class Partida
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime IniciadaEm { get; set; } = DateTime.UtcNow;

    public DateTime? FinalizadaEm { get; set; }

    public StatusPartida Status { get; set; } = StatusPartida.Aguardando;

    public ICollection<ParticipacaoPartida> Participacoes { get; set; }
        = new List<ParticipacaoPartida>();
}
