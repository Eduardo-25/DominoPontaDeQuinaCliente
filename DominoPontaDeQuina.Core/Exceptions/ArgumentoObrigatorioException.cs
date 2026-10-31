namespace DominoPontaDeQuina.Core.Exceptions;

public class ArgumentoObrigatorioException : ArgumentNullException
{
    public ArgumentoObrigatorioException(
        string paramName,
        string message)
        : base(paramName, message)
    {
    }
}