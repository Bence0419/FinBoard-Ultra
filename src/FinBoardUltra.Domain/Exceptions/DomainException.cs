namespace FinBoardUltra.Domain.Exceptions;

/// <summary>Base class for all exceptions originating in the domain layer.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
