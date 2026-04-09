namespace FinBoardUltra.Domain.Interfaces.Services;

/// <summary>
/// Abstracts password hashing so the Application layer never depends directly on BCrypt.
/// Implemented in Infrastructure by BcryptPasswordHasher.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Returns an adaptive one-way hash of <paramref name="plaintext"/>.</summary>
    string Hash(string plaintext);

    /// <summary>Returns true if <paramref name="plaintext"/> matches the stored <paramref name="hash"/>.</summary>
    bool Verify(string plaintext, string hash);
}
