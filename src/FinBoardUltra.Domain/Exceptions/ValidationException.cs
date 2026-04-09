namespace FinBoardUltra.Domain.Exceptions;

/// <summary>Thrown when input is invalid or a domain invariant is broken.</summary>
public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }
}
