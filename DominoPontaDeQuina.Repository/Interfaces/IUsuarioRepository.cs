using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Repository.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorIdAsync(Guid id);

    Task<Usuario?> BuscarPorEmailAsync(string email);

    Task<List<Usuario>> ListarAsync();

    Task<List<Usuario>> BuscarPorNomeAsync(string nome);

    Task AdicionarAsync(Usuario usuario);

    Task AtualizarAsync(Usuario usuario);

    Task ExcluirAsync(Guid id);
}