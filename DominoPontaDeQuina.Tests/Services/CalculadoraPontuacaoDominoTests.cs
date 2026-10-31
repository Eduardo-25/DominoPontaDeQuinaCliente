using DominoPontaDeQuina.Core.Services;
using FluentAssertions;
using Xunit;

namespace DominoPontaDeQuina.Tests.Services;

public class CalculadoraPontuacaoDominoTests
{
    [Theory]
    [InlineData(10, 2)]
    [InlineData(15, 3)]
    [InlineData(20, 4)]
    public void CalcularPontos_SomaMultiplaDeCinco_DeveRetornarPontosCorretos(
        int soma,
        int pontosEsperados)
    {
        // Arrange
        // Os valores de entrada e os resultados esperados
        // estão definidos nos InlineData.

        // Act
        var resultado = CalculadoraPontuacaoDomino.CalcularPontos(soma);

        // Assert
        resultado.Should().Be(pontosEsperados);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(12)]
    public void CalcularPontos_SomaNaoMultiplaDeCinco_DeveRetornarZero(
        int soma)
    {
        // Arrange

        // Act
        var resultado = CalculadoraPontuacaoDomino.CalcularPontos(soma);

        // Assert
        resultado.Should().Be(0);
    }

    [Fact]
    public void CalcularPontos_SomaNegativa_DeveLancarExcecao()
    {
        // Arrange
        var soma = -5;

        // Act
        Action acao = () =>
            CalculadoraPontuacaoDomino.CalcularPontos(soma);

        // Assert
        acao.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("soma");
    }
}