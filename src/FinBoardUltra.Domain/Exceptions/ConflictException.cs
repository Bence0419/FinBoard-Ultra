namespace FinBoardUltra.Domain.Exceptions;

/// <summary>Thrown on uniqueness violations (e.g. duplicate email on registration).</summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}
