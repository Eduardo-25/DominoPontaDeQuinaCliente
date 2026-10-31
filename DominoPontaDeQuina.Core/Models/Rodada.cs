using DominoPontaDeQuina.Core.Enums;
using DominoPontaDeQuina.Core.Interfaces;
using DominoPontaDeQuina.Core.Services;
using System.Collections.ObjectModel;
using DominoPontaDeQuina.Core.Exceptions;
namespace DominoPontaDeQuina.Core.Models;

/// <inheritdoc cref="IRodada"/>
public class Rodada() : IRodada
{
    private readonly Stack<Jogada> Jogadas = [];

    private Queue<MaoJogador> _jogadores = [];

    private readonly Dictionary<Jogador, int> _pontuacaoPorJogador = [];

    private Jogador? _vencedor;

    /// <inheritdoc />
    public Tabuleiro Tabuleiro { get; } = new();

    /// <summary>
    /// Pontuação acumulada por jogador nesta rodada.
    /// </summary>
    public IReadOnlyDictionary<Jogador, int> PontuacaoPorJogador =>
        _pontuacaoPorJogador;

    /// <inheritdoc />
    public ReadOnlyCollection<Jogada> HistoricoJogadas =>
        Jogadas.ToList().AsReadOnly();

    /// <inheritdoc />
    public MaoJogador JogadorAtual => _jogadores.Peek();

    /// <inheritdoc />
    public StatusRodada Status { get; private set; } =
        StatusRodada.NaoIniciada;

    /// <inheritdoc />
    public TipoFinalizacaoRodada? TipoFinalizacao { get; private set; }

    /// <inheritdoc />
    public void Iniciar(
        ReadOnlyCollection<Jogador> jogadores,
        Rodada? rodadaAnterior = null)
    {
        ArgumentNullException.ThrowIfNull(jogadores);

        var maosJogadores = DistribuirPecas(jogadores);

        var primeiroJogador = GetPrimeiroJogador(
            maosJogadores,
            rodadaAnterior
        );

        OrganizaJogadores(maosJogadores, primeiroJogador);

        Jogadas.Clear();
        Tabuleiro.Limpar();
        _pontuacaoPorJogador.Clear();
        _vencedor = null;
        TipoFinalizacao = null;

        Status = StatusRodada.EmAndamento;
    }

    /// <inheritdoc />
    public void RegistrarJogada(Jogada jogada)
    {
            if (jogada is null)
    {
        throw new ArgumentoObrigatorioException(
            nameof(jogada),
            "A jogada não pode ser nula."
        );
    }

        if (Status != StatusRodada.EmAndamento)
        {
            throw new InvalidOperationException(
                "A rodada não está em andamento."
            );
        }

        if (jogada.EhPassarVez())
        {
            jogada.MarcarComoAplicada();
            Jogadas.Push(jogada);
            return;
        }

        if (jogada.Peca is not Peca peca ||
            jogada.Lado is not LadoTabuleiro lado)
        {
            jogada.MarcarComoInvalida();

            throw new InvalidOperationException(
                "Uma jogada válida deve informar a peça e o lado."
            );
        }

        if (!Tabuleiro.PodeColar(peca, lado))
        {
            jogada.MarcarComoInvalida();

            throw new InvalidOperationException(
                "A peça não é compatível com a ponta escolhida."
            );
        }

        Tabuleiro.Colar(peca, lado);

        var maoDoJogador = _jogadores.FirstOrDefault(
            mao => mao.Jogador.Id == jogada.Jogador.Id
        );

        // Retira a peça da mão quando ela pertence a um jogador
        // que participa desta rodada.
        maoDoJogador?.RemoverPeca(peca);

        jogada.MarcarComoAplicada();
        Jogadas.Push(jogada);

        CalcularPontuacao(jogada.Jogador);
    }

    /// <inheritdoc />
    public bool VerificarBatida()
    {
        if (Status != StatusRodada.EmAndamento ||
            _jogadores.Count == 0)
        {
            return false;
        }

        var maoAtual = _jogadores.Peek();

        if (!maoAtual.EstaSemPecas())
        {
            return false;
        }

        _vencedor = maoAtual.Jogador;
        TipoFinalizacao = TipoFinalizacaoRodada.JogadorBateu;
        Status = StatusRodada.Finalizada;

        return true;
    }

    /// <inheritdoc />
    public bool VerificarTabuleiroTravado()
    {
        if (Status != StatusRodada.EmAndamento ||
            _jogadores.Count == 0)
        {
            return false;
        }

        if (!Tabuleiro.EstaTravado(_jogadores))
        {
            return false;
        }

        _vencedor = _jogadores
            .OrderBy(mao => mao.SomarPecasNaMao())
            .Select(mao => mao.Jogador)
            .FirstOrDefault();

        TipoFinalizacao = TipoFinalizacaoRodada.TabuleiroTravado;
        Status = StatusRodada.Finalizada;

        return true;
    }

    /// <inheritdoc />
    public Jogador? GetVencedor()
    {
        if (_vencedor is not null)
        {
            return _vencedor;
        }

        if (TipoFinalizacao == TipoFinalizacaoRodada.TabuleiroTravado)
        {
            return _jogadores
                .OrderBy(mao => mao.SomarPecasNaMao())
                .Select(mao => mao.Jogador)
                .FirstOrDefault();
        }

        return null;
    }

    /// <summary>
    /// Distribui sete peças para cada jogador.
    /// </summary>
    private List<MaoJogador> DistribuirPecas(
        ReadOnlyCollection<Jogador> jogadores)
    {
        if (jogadores.Count == 0)
        {
            throw new ArgumentException(
                "A rodada precisa ter pelo menos um jogador.",
                nameof(jogadores)
            );
        }

        if (jogadores.Count > 4)
        {
            throw new ArgumentException(
                "O dominó de 28 peças comporta até quatro jogadores com sete peças cada.",
                nameof(jogadores)
            );
        }

        var pecas = new List<Peca>();

        for (int valorA = 0; valorA <= 6; valorA++)
        {
            for (int valorB = valorA; valorB <= 6; valorB++)
            {
                pecas.Add(new Peca(valorA, valorB));
            }
        }

        // Embaralhamento Fisher-Yates.
        for (int i = pecas.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (pecas[i], pecas[j]) = (pecas[j], pecas[i]);
        }

        int quantidadeDistribuida = Math.Min(
            jogadores.Count * 7,
            pecas.Count
        );

        // Garante que a sena esteja entre as peças distribuídas.
        int indiceSena = pecas.FindIndex(peca => peca.EhSena);

        if (indiceSena >= quantidadeDistribuida)
        {
            int ultimoIndiceDistribuido = quantidadeDistribuida - 1;

            (pecas[indiceSena], pecas[ultimoIndiceDistribuido]) =
                (pecas[ultimoIndiceDistribuido], pecas[indiceSena]);
        }

        var maos = new List<MaoJogador>();

        for (int i = 0; i < jogadores.Count; i++)
        {
            var mao = new MaoJogador(jogadores[i]);

            for (int j = 0; j < 7; j++)
            {
                int indicePeca = i * 7 + j;

                if (indicePeca < quantidadeDistribuida)
                {
                    mao.AdicionarPeca(pecas[indicePeca]);
                }
            }

            maos.Add(mao);
        }

        return maos;
    }

    /// <summary>
    /// Determina quem começa a rodada.
    /// </summary>
    private Jogador GetPrimeiroJogador(
        List<MaoJogador> jogadores,
        Rodada? rodadaAnterior = null)
    {
        if (rodadaAnterior is not null)
        {
            return rodadaAnterior.GetVencedor()
                ?? throw new InvalidOperationException(
                    "Não foi possível identificar o vencedor da rodada anterior."
                );
        }

        var jogadorComSena = jogadores.FirstOrDefault(
            mao => mao.PossuiSena()
        );

        if (jogadorComSena is not null)
        {
            return jogadorComSena.Jogador;
        }

        // Alternativa caso nenhuma mão possua a sena.
        return jogadores[0].Jogador;
    }

    /// <summary>
    /// Organiza a ordem de jogada a partir do primeiro jogador.
    /// </summary>
    private void OrganizaJogadores(
        List<MaoJogador> jogadores,
        Jogador primeiroJogador)
    {
        int indiceInicial = jogadores.FindIndex(
            mao => mao.Jogador.Id == primeiroJogador.Id
        );

        if (indiceInicial < 0)
        {
            throw new InvalidOperationException(
                "O primeiro jogador não pertence à rodada."
            );
        }

        var ordem = jogadores
            .Skip(indiceInicial)
            .Concat(jogadores.Take(indiceInicial));

        _jogadores = new Queue<MaoJogador>(ordem);
    }

    /// <summary>
    /// Calcula os pontos gerados pelas pontas do tabuleiro.
    /// </summary>
    private void CalcularPontuacao(Jogador jogador)
    {
        int soma = Tabuleiro.SomarPontasExternas();

        int pontos = CalculadoraPontuacaoDomino.CalcularPontos(soma);

        if (pontos == 0)
        {
            return;
        }

        _pontuacaoPorJogador.TryGetValue(
            jogador,
            out int pontuacaoAtual
        );

        _pontuacaoPorJogador[jogador] = pontuacaoAtual + pontos;
    }
}