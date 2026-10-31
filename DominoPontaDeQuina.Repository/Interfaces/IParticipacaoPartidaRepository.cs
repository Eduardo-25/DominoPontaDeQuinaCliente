using DominoPontaDeQuina.Domain.Entities;

namespace DominoPontaDeQuina.Repository.Interfaces;

public interface IParticipacaoPartidaRepository
{
    Task<ParticipacaoPartida?> BuscarPorIdAsync(Guid id);

    Task<List<ParticipacaoPartida>> ListarAsync();

    Task<List<ParticipacaoPartida>> BuscarPorPartidaAsync(Guid partidaId);

    Task<List<ParticipacaoPartida>> BuscarPorJogadorAsync(Guid jogadorId);

    Task<List<ParticipacaoPartida>> ListarVencedoresAsync();

    Task AdicionarAsync(ParticipacaoPartida participacao);

    Task AtualizarAsync(ParticipacaoPartida participacao);

    Task ExcluirAsync(Guid id);
}