using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Interfaces;

namespace DominoPontaDeQuina.Core.Models;

/// <inheritdoc cref="IJogada"/>
public class Jogada(
    Jogador jogador,
    Peca? peca = null,
    int? valorColado = null,
    LadoTabuleiro? lado = null) : IJogada
{
    /// <inheritdoc />
    public Jogador Jogador { get; } = jogador;

    /// <inheritdoc />
    public Peca? Peca { get; } = peca;

    /// <summary>
    /// Valor utilizado na peça durante a jogada, quando informado.
    /// </summary>
    public int? ValorColado { get; } = valorColado;

    /// <inheritdoc />
    public LadoTabuleiro? Lado { get; } = lado;

    /// <inheritdoc />
    public StatusJogada Status { get; private set; } =
        StatusJogada.Pendente;

    /// <inheritdoc />
    public bool EhPassarVez() =>
        Peca is null && Lado is null;

    /// <inheritdoc />
    public void MarcarComoAplicada() =>
        Status = StatusJogada.Aplicada;

    /// <inheritdoc />
    public void MarcarComoInvalida() =>
        Status = StatusJogada.Invalida;
}