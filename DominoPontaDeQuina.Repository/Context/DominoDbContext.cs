using DominoPontaDeQuina.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DominoPontaDeQuina.Repository.Context;

public class DominoDbContext : DbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Jogador> Jogadores => Set<Jogador>();
    public DbSet<Partida> Partidas => Set<Partida>();
    public DbSet<ParticipacaoPartida> ParticipacoesPartidas => Set<ParticipacaoPartida>();

    private static string ObterCaminhoBanco()
    {
        var diretorioSolucao = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                ".."
            )
        );

        return Path.Combine(diretorioSolucao, "domino.db");
    }

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var caminhoBanco = ObterCaminhoBanco();

            optionsBuilder.UseSqlite(
                $"Data Source={caminhoBanco}",
                options => options.MigrationsAssembly(
                    "DominoPontaDeQuina.Migrations"));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Nome)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(u => u.HashSenha)
                .IsRequired()
                .HasMaxLength(500);
        });

        modelBuilder.Entity<Jogador>(entity =>
        {
            entity.HasKey(j => j.Id);

            entity.Property(j => j.NomeExibicao)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(j => j.Usuario)
                .WithMany(u => u.Jogadores)
                .HasForeignKey(j => j.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Partida>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.IniciadaEm)
                .IsRequired();

            entity.Property(p => p.Status)
                .IsRequired();
        });

        modelBuilder.Entity<ParticipacaoPartida>(entity =>
        {
            entity.HasKey(pp => pp.Id);

            entity.HasOne(pp => pp.Partida)
                .WithMany(p => p.Participacoes)
                .HasForeignKey(pp => pp.PartidaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pp => pp.Jogador)
                .WithMany(j => j.Participacoes)
                .HasForeignKey(pp => pp.JogadorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(pp => pp.Posicao)
                .IsRequired();

            entity.Property(pp => pp.Pontuacao)
                .IsRequired();

            entity.Property(pp => pp.Vencedor)
                .IsRequired();
        });
    }
}