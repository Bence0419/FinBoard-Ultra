using FinBoardUltra.Domain.Entities;
using FinBoardUltra.Domain.Enums;

namespace FinBoardUltra.Domain.Interfaces.Repositories;

public interface IAuditLogRepository
{
    /// <summary>Appends an audit entry. Rows are never updated or deleted.</summary>
    Task AddAsync(AuditLog entry);

    /// <summary>
    /// Returns the count of LoginFailed events within the given time window for the specified IP address.
    /// Used for rate limiting at the service layer.
    /// </summary>
    Task<int> CountRecentFailedLoginsAsync(string? ipAddress, DateTime since);
}
