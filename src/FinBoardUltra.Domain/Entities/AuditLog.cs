using FinBoardUltra.Domain.Enums;

namespace FinBoardUltra.Domain.Entities;

/// <summary>
/// Append-only audit record. Rows are never updated or deleted.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>Null for pre-authentication events (e.g. failed login with unknown email).</summary>
    public Guid? UserId { get; set; }

    public AuditAction Action { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>Free-text or JSON with event-specific context.</summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>IP address placeholder. Supplied by the calling layer; null when not available.</summary>
    public string? IpAddress { get; set; }
}
