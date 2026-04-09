namespace FinBoardUltra.Domain.Exceptions;

/// <summary>Thrown when a rate limit is exceeded (e.g. too many failed login attempts).</summary>
public class TooManyRequestsException : DomainException
{
    public TooManyRequestsException(string message) : base(message) { }
}
