using DominoPontaDeQuina.Client.Services;
using FluentAssertions;
using Xunit;

namespace DominoPontaDeQuina.Client.Tests;

public class GameStateParserTests
{
    [Fact]
    public void Parse_DeveLerMaoPropriaMesaPontasEVersao()
    {
        // Arrange
        const string json = """
        {
          "jogadorId": "perfil-1",
          "mao": [{ "valorA": 6, "valorB": 3 }],
          "tabuleiro": { "pecas": [{ "valorA": 2, "valorB": 4 }] },
          "pontaEsquerda": 2,
          "pontaDireita": 4,
          "versaoEstado": 7,
          "proximaAcao": "Jogar",
          "minhaVez": true,
          "placar": { "perfil-1": 5, "perfil-2": 3 }
        }
        """;

        // Act
        var state = GameStateSnapshot.Parse(json, "perfil-1");

        // Assert
        state.TemEstado.Should().BeTrue();
        state.MinhaMao.Should().ContainSingle().Which.Label.Should().Be("[6|3]");
        state.Mesa.Should().ContainSingle().Which.Label.Should().Be("[2|4]");
        state.PontaEsquerda.Should().Be(2);
        state.PontaDireita.Should().Be(4);
        state.Versao.Should().Be(7);
        state.MinhaVez.Should().BeTrue();
    }

    [Fact]
    public void Parse_NaoDeveMostrarMaoDoAdversario()
    {
        // Arrange
        const string json = """
        {
          "participantes": [
            { "id": "perfil-1", "mao": [{ "valorA": 6, "valorB": 3 }] },
            { "id": "perfil-2", "mao": [{ "valorA": 1, "valorB": 1 }, { "valorA": 5, "valorB": 5 }] }
          ],
          "tabuleiro": { "pecas": [{ "valorA": 2, "valorB": 4 }] },
          "minhaVez": true,
          "versaoEstado": 1
        }
        """;

        // Act
        var state = GameStateSnapshot.Parse(json, "perfil-1");

        // Assert
        state.MinhaMao.Should().ContainSingle().Which.Label.Should().Be("[6|3]");
        state.MinhaMao.Should().NotContain(piece => piece.Label == "[1|1]" || piece.Label == "[5|5]");
    }

    [Fact]
    public void Autonomo_DeveEscolherPecaQueEncaixaNaDireita()
    {
        // Arrange
        const string json = """
        {
          "jogadorId": "perfil-1",
          "mao": [
            { "valorA": 0, "valorB": 1 },
            { "valorA": 2, "valorB": 5 }
          ],
          "tabuleiro": { "pecas": [{ "valorA": 3, "valorB": 2 }] },
          "pontaEsquerda": 3,
          "pontaDireita": 2,
          "minhaVez": true,
          "proximaAcao": "Jogar"
        }
        """;
        var state = GameStateSnapshot.Parse(json, "perfil-1");

        // Act
        var decision = DominoAutonomousPlayer.Decide(state);

        // Assert
        decision.Type.Should().Be("play");
        decision.Piece.Should().Be(new DominoPieceView(2, 5));
        decision.Side.Should().Be(1);
    }

    [Fact]
    public void Autonomo_DeveEscolherPecaQueEncaixaNaEsquerda()
    {
        // Arrange
        const string json = """
        {
          "jogadorId": "perfil-1",
          "mao": [
            { "valorA": 3, "valorB": 4 },
            { "valorA": 2, "valorB": 5 }
          ],
          "tabuleiro": { "pecas": [{ "valorA": 3, "valorB": 2 }] },
          "pontaEsquerda": 3,
          "pontaDireita": 2,
          "minhaVez": true,
          "proximaAcao": "Jogar"
        }
        """;
        var state = GameStateSnapshot.Parse(json, "perfil-1");

        // Act
        var decision = DominoAutonomousPlayer.Decide(state);

        // Assert
        decision.Type.Should().Be("play");
        decision.Piece.Should().Be(new DominoPieceView(3, 4));
        decision.Side.Should().Be(0);
    }

    [Fact]
    public void Autonomo_DeveComprarQuandoNaoHaEncaixeEAApiPermiteCompra()
    {
        // Arrange
        const string json = """
        {
          "jogadorId": "perfil-1",
          "mao": [{ "valorA": 0, "valorB": 1 }],
          "pontaEsquerda": 2,
          "pontaDireita": 4,
          "minhaVez": true,
          "podeComprar": true,
          "proximaAcao": "Comprar"
        }
        """;
        var state = GameStateSnapshot.Parse(json, "perfil-1");

        // Act
        var decision = DominoAutonomousPlayer.Decide(state);

        // Assert
        decision.Type.Should().Be("buy");
    }

    [Fact]
    public void Autonomo_NaoDeveEnviarAcaoQuandoNaoForMinhaVez()
    {
        // Arrange
        const string json = """
        {
          "jogadorId": "perfil-1",
          "mao": [{ "valorA": 6, "valorB": 3 }],
          "pontaEsquerda": 3,
          "pontaDireita": 4,
          "minhaVez": false,
          "proximaAcao": "Aguardar"
        }
        """;
        var state = GameStateSnapshot.Parse(json, "perfil-1");

        // Act
        var decision = DominoAutonomousPlayer.Decide(state);

        // Assert
        decision.Type.Should().Be("wait");
    }
}
