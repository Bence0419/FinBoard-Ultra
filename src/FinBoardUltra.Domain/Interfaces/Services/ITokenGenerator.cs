namespace FinBoardUltra.Domain.Interfaces.Services;

/// <summary>
/// Generates cryptographically random tokens.
/// Implemented in Infrastructure by SecureTokenGenerator (RandomNumberGenerator, 256-bit).
/// </summary>
public interface ITokenGenerator
{
    /// <summary>Returns a URL-safe base-64 string with 256 bits of entropy.</summary>
    string Generate();
}
