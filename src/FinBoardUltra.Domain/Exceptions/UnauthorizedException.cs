namespace FinBoardUltra.Domain.Exceptions;

/// <summary>Thrown when an operation is attempted without a valid authenticated session.</summary>
public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message) : base(message) { }
}
