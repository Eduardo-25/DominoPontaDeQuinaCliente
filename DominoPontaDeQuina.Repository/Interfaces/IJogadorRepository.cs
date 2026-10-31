using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Repository.Interfaces;

public interface IJogadorRepository
{
    Task<Jogador?> BuscarPorIdAsync(Guid id);

    Task<List<Jogador>> ListarAsync();

    Task<List<Jogador>> BuscarPorNomeAsync(string nome);

    Task<List<Jogador>> BuscarPorUsuarioAsync(Guid usuarioId);

    Task AdicionarAsync(Jogador jogador);

    Task AtualizarAsync(Jogador jogador);

    Task ExcluirAsync(Guid id);
}