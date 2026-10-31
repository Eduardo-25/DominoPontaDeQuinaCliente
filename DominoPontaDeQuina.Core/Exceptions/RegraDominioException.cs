namespace DominoPontaDeQuina.Core.Exceptions;

public class RegraDominioException : InvalidOperationException
{
    public RegraDominioException(string message)
        : base(message)
    {
    }
}