using System.ComponentModel.DataAnnotations;

namespace DominoPontaDeQuina.Domain.Entities;

public class Jogador
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string NomeExibicao { get; set; } = string.Empty;

    [Required]
    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public ICollection<ParticipacaoPartida> Participacoes { get; set; }
        = new List<ParticipacaoPartida>();
}
