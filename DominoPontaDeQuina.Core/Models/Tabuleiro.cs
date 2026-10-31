using DominoPontaDeQuina.Core.Enums;

namespace DominoPontaDeQuina.Core.Models;

/// <summary>
/// Representa o tabuleiro da rodada de dominó.
/// </summary>
public class Tabuleiro
{
    /// <summary>
    /// Peças posicionadas na ordem em que foram coladas.
    /// </summary>
    public List<Peca> Pecas { get; } = [];

    /// <summary>
    /// Indica se o tabuleiro está vazio.
    /// </summary>
    public bool EstaVazio => Pecas.Count == 0;

    /// <summary>
    /// Valor exposto na ponta esquerda.
    /// </summary>
    public int? PontaEsquerda =>
        EstaVazio ? null : Pecas[0].ValorA;

    /// <summary>
    /// Valor exposto na ponta direita.
    /// </summary>
    public int? PontaDireita =>
        EstaVazio ? null : Pecas[^1].ValorB;

    /// <summary>
    /// Verifica se uma peça pode ser colada no lado informado.
    /// </summary>
    public bool PodeColar(Peca peca, LadoTabuleiro lado)
    {
        if (!Enum.IsDefined(typeof(LadoTabuleiro), lado))
        {
            return false;
        }

        if (EstaVazio)
        {
            return true;
        }

        return lado switch
        {
            LadoTabuleiro.Esquerda =>
                PontaEsquerda.HasValue &&
                peca.PossuiValor(PontaEsquerda.Value),

            LadoTabuleiro.Direita =>
                PontaDireita.HasValue &&
                peca.PossuiValor(PontaDireita.Value),

            _ => false
        };
    }

    /// <summary>
    /// Cola uma peça no lado escolhido.
    /// Inverte a peça quando necessário.
    /// </summary>
    public void Colar(Peca peca, LadoTabuleiro lado)
    {
        if (!PodeColar(peca, lado))
        {
            throw new InvalidOperationException(
                "A peça não é compatível com a ponta escolhida."
            );
        }

        if (EstaVazio)
        {
            Pecas.Add(peca);
            return;
        }

        if (lado == LadoTabuleiro.Esquerda)
        {
            int ponta = PontaEsquerda!.Value;

            if (peca.ValorB == ponta)
            {
                Pecas.Insert(0, peca);
            }
            else
            {
                Pecas.Insert(0, peca.Inverter());
            }
        }
        else
        {
            int ponta = PontaDireita!.Value;

            if (peca.ValorA == ponta)
            {
                Pecas.Add(peca);
            }
            else
            {
                Pecas.Add(peca.Inverter());
            }
        }
    }

    /// <summary>
    /// Soma as pontas externas do tabuleiro.
    /// </summary>
    public int SomarPontasExternas()
    {
        if (EstaVazio)
        {
            return 0;
        }

        return PontaEsquerda!.Value + PontaDireita!.Value;
    }

    /// <summary>
    /// Indica se nenhuma mão possui peça compatível para jogar.
    /// </summary>
    public bool EstaTravado(IEnumerable<MaoJogador> maosJogadores)
    {
        ArgumentNullException.ThrowIfNull(maosJogadores);

        return !maosJogadores.Any(mao =>
            mao is not null && mao.PodeJogar(this));
    }

    /// <summary>
    /// Limpa o tabuleiro.
    /// </summary>
    public void Limpar()
    {
        Pecas.Clear();
    }
}