namespace DominoPontaDeQuina.Core.Services;

public static class CalculadoraPontuacaoDomino
{
    public static int CalcularPontos(int soma)
    {
        if (soma < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(soma),
                "A soma não pode ser negativa."
            );
        }

        if (soma % 5 != 0)
            return 0;

        return soma / 5;
    }
}