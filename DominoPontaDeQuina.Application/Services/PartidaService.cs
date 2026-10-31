using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Interfaces;

namespace DominoPontaDeQuina.Application.Services;

public class PartidaService
{
    private readonly IPartidaRepository _partidaRepository;

    public PartidaService(IPartidaRepository partidaRepository)
    {
        _partidaRepository = partidaRepository;
    }

    public async Task<List<Partida>> ListarAsync()
    {
        return await _partidaRepository.ListarAsync();
    }

    public async Task<Partida?> BuscarPorIdAsync(Guid id)
    {
        return await _partidaRepository.BuscarPorIdAsync(id);
    }

    public async Task<List<Partida>> ListarEmAndamentoAsync()
    {
        return await _partidaRepository.ListarEmAndamentoAsync();
    }

    public async Task<Partida> CriarAsync()
    {
        var partida = new Partida
        {
            Status = StatusPartida.Aguardando,
            IniciadaEm = DateTime.UtcNow
        };

        await _partidaRepository.AdicionarAsync(partida);

        return partida;
    }

    public async Task IniciarAsync(Guid partidaId)
    {
        var partida =
            await _partidaRepository.BuscarPorIdAsync(partidaId);

        if (partida is null)
        {
            throw new InvalidOperationException(
                "Partida não encontrada.");
        }

        if (partida.Status != StatusPartida.Aguardando)
        {
            throw new InvalidOperationException(
                "A partida não está aguardando início.");
        }

        partida.Status = StatusPartida.EmAndamento;

        await _partidaRepository.AtualizarAsync(partida);
    }

    public async Task FinalizarAsync(Guid partidaId)
    {
        var partida =
            await _partidaRepository.BuscarPorIdAsync(partidaId);

        if (partida is null)
        {
            throw new InvalidOperationException(
                "Partida não encontrada.");
        }

        partida.Status = StatusPartida.Finalizado;
        partida.FinalizadaEm = DateTime.UtcNow;

        await _partidaRepository.AtualizarAsync(partida);
    }
}