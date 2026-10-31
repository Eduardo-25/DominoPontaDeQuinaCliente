using System.ComponentModel.DataAnnotations;

namespace DominoPontaDeQuina.Domain.Entities;

public class Usuario
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string HashSenha { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Jogador> Jogadores { get; set; }
        = new List<Jogador>();
}
