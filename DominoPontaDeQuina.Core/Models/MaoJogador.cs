using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Interfaces;

namespace DominoPontaDeQuina.Core.Models;

/// <inheritdoc cref="IMaoJogador"/>
public class MaoJogador(Jogador jogador) : IMaoJogador
{
    private readonly List<Peca> _pecas = [];

    /// <summary>
    /// Peças atualmente disponíveis na mão.
    /// </summary>
    public IReadOnlyList<Peca> Pecas => _pecas.AsReadOnly();

    /// <inheritdoc />
    public Jogador Jogador { get; } =
        jogador ?? throw new ArgumentNullException(nameof(jogador));

    /// <inheritdoc />
    public void AdicionarPeca(Peca peca)
    {
        _pecas.Add(peca);
    }

    /// <summary>
    /// Remove uma peça da mão.
    /// Retorna false se a peça não estiver presente.
    /// </summary>
    public bool RemoverPeca(Peca peca)
    {
        return _pecas.Remove(peca);
    }

    /// <summary>
    /// Verifica se existe alguma peça compatível com o tabuleiro.
    /// </summary>
    public bool PodeJogar(Tabuleiro tabuleiro)
    {
        ArgumentNullException.ThrowIfNull(tabuleiro);

        return _pecas.Any(peca =>
            tabuleiro.PodeColar(peca, LadoTabuleiro.Esquerda) ||
            tabuleiro.PodeColar(peca, LadoTabuleiro.Direita));
    }

    /// <inheritdoc />
    public int SomarPecasNaMao()
    {
        return _pecas.Sum(peca => peca.SomaValores);
    }

    /// <inheritdoc />
    public bool PossuiSena()
    {
        return _pecas.Any(peca => peca.EhSena);
    }

    /// <inheritdoc />
    public bool EstaSemPecas()
    {
        return _pecas.Count == 0;
    }

    /// <inheritdoc />
    public Jogada GetJogada(Tabuleiro tabuleiro)
    {
        ArgumentNullException.ThrowIfNull(tabuleiro);

        foreach (var peca in _pecas)
        {
            if (tabuleiro.EstaVazio)
            {
                return new Jogada(
                    Jogador,
                    peca,
                    peca.ValorA,
                    LadoTabuleiro.Esquerda
                );
            }

            if (tabuleiro.PontaEsquerda is int esquerda &&
                peca.PossuiValor(esquerda))
            {
                return new Jogada(
                    Jogador,
                    peca,
                    esquerda,
                    LadoTabuleiro.Esquerda
                );
            }

            if (tabuleiro.PontaDireita is int direita &&
                peca.PossuiValor(direita))
            {
                return new Jogada(
                    Jogador,
                    peca,
                    direita,
                    LadoTabuleiro.Direita
                );
            }
        }

        // Não existe peça compatível: passar a vez.
        return new Jogada(Jogador);
    }

    /// <inheritdoc />
    public void DefazerJogada(Jogada jogada)
    {
        ArgumentNullException.ThrowIfNull(jogada);

        if (jogada.Jogador.Id != Jogador.Id)
        {
            throw new InvalidOperationException(
                "A jogada pertence a outro jogador."
            );
        }

        if (jogada.Peca is Peca peca && !_pecas.Contains(peca))
        {
            _pecas.Add(peca);
        }
    }
}