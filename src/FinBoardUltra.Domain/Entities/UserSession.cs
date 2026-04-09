namespace FinBoardUltra.Domain.Entities;

public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>256-bit random token (base-64 encoded). Primary session credential.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>256-bit random refresh token used to issue a new Token after expiry.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    /// <summary>Set on logout or password change. Sessions are never hard-deleted.</summary>
    public bool IsRevoked { get; set; }

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}
