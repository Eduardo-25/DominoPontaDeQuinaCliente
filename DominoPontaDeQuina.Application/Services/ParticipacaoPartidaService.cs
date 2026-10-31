using DominoPontaDeQuina.Domain.Entities;
using DominoPontaDeQuina.Repository.Interfaces;

namespace DominoPontaDeQuina.Application.Services;

public class ParticipacaoPartidaService
{
    private readonly IParticipacaoPartidaRepository
        _participacaoRepository;

    public ParticipacaoPartidaService(
        IParticipacaoPartidaRepository participacaoRepository)
    {
        _participacaoRepository = participacaoRepository;
    }

    public async Task<List<ParticipacaoPartida>>
        BuscarPorPartidaAsync(Guid partidaId)
    {
        return await _participacaoRepository
            .BuscarPorPartidaAsync(partidaId);
    }

    public async Task<List<ParticipacaoPartida>>
        BuscarPorJogadorAsync(Guid jogadorId)
    {
        return await _participacaoRepository
            .BuscarPorJogadorAsync(jogadorId);
    }

    public async Task AdicionarAsync(
        Guid partidaId,
        Guid jogadorId,
        int posicao)
    {
        var participacao = new ParticipacaoPartida
        {
            PartidaId = partidaId,
            JogadorId = jogadorId,
            Posicao = posicao,
            Pontuacao = 0,
            Vencedor = false
        };

        await _participacaoRepository
            .AdicionarAsync(participacao);
    }
}