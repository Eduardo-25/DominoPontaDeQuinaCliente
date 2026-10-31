using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Repository.Interfaces;

public interface IPartidaRepository
{
    Task<Partida?> BuscarPorIdAsync(Guid id);

    Task<List<Partida>> ListarAsync();

    Task<List<Partida>> BuscarPorStatusAsync(StatusPartida status);

    Task<List<Partida>> ListarEmAndamentoAsync();

    Task AdicionarAsync(Partida partida);

    Task AtualizarAsync(Partida partida);

    Task ExcluirAsync(Guid id);
}