using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Exceptions;
using DominoPontaDeQuina.Core.Interfaces;
using System.Collections.ObjectModel;

namespace DominoPontaDeQuina.Core.Models;

/// <summary>
/// Representa uma partida de dominó.
/// </summary>
public class Partida(int pontuacaoAlvo = 50) : IPartida
{
    private readonly Stack<Rodada> _rodadas = [];

    public int PontuacaoAlvo { get; } = pontuacaoAlvo;

    public StatusPartida Status { get; protected set; } =
        StatusPartida.NaoIniciada;

    public List<Time> Times { get; } = [];

    public ReadOnlyCollection<Rodada> HistoricoRodadas =>
        _rodadas.ToList().AsReadOnly();

    public Rodada? RodadaAtual =>
        _rodadas.Count > 0 ? _rodadas.Peek() : null;

    /// <summary>
    /// Retorna a pontuação de todos os times.
    /// </summary>
    public Dictionary<Time, int> GetPontuacaoTimes()
    {
        return Times.ToDictionary(
            time => time,
            time => time.Pontuacao
        );
    }

    /// <summary>
    /// Retorna o time vencedor, quando algum atingir a pontuação-alvo.
    /// </summary>
    public Time? GetTimeVencedor()
    {
        return Times
            .Where(time => time.Pontuacao >= PontuacaoAlvo)
            .OrderByDescending(time => time.Pontuacao)
            .FirstOrDefault();
    }

    /// <summary>
    /// Verifica se algum time atingiu a pontuação-alvo.
    /// </summary>
    public bool VerificaPontuacaoAlvoAtingida()
    {
        return Times.Any(time => time.Pontuacao >= PontuacaoAlvo);
    }

    /// <summary>
    /// Inicia uma nova rodada.
    /// </summary>
    public void IniciarNovaRodada()
    {
        if (Status == StatusPartida.Finalizada)
        {
            throw new RegraDominioException(
                "Não é possível iniciar uma nova rodada em uma partida finalizada."
            );
        }

        Status = StatusPartida.EmAndamento;
        _rodadas.Push(new Rodada());
    }

    /// <summary>
    /// Finaliza a partida se a pontuação-alvo tiver sido atingida.
    /// </summary>
    public void FinalizarPartida()
    {
        if (Status != StatusPartida.EmAndamento)
        {
            throw new RegraDominioException(
                "Não é possível finalizar uma partida que não está em andamento."
            );
        }

        if (VerificaPontuacaoAlvoAtingida())
        {
            Status = StatusPartida.Finalizada;
        }
    }
}