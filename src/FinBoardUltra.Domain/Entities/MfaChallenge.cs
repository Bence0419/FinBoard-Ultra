namespace FinBoardUltra.Domain.Entities;

public class MfaChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Six-digit random numeric code.</summary>
    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>CreatedAt + 10 minutes.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Null until the code is successfully verified (single-use).</summary>
    public DateTime? UsedAt { get; set; }

    public bool IsValid => UsedAt is null && DateTime.UtcNow < ExpiresAt;
}
