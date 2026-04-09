namespace FinBoardUltra.Domain.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist or is not visible to the
/// current user. Callers cannot distinguish between "missing" and "forbidden"
/// — this is intentional to prevent user enumeration.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}
