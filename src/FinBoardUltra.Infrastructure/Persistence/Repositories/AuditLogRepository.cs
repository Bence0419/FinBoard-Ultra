using FinBoardUltra.Domain.Entities;
using FinBoardUltra.Domain.Enums;
using FinBoardUltra.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinBoardUltra.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog entry)
    {
        if (entry.Id == Guid.Empty) entry.Id = Guid.NewGuid();
        if (entry.Timestamp == default) entry.Timestamp = DateTime.UtcNow;

        await db.AuditLogs.AddAsync(entry);
        await db.SaveChangesAsync();
    }

    public async Task<int> CountRecentFailedLoginsAsync(string? ipAddress, DateTime since)
    {
        var query = db.AuditLogs
            .Where(e => e.Action == AuditAction.LoginFailed && e.Timestamp >= since);

        if (ipAddress is not null)
            query = query.Where(e => e.IpAddress == ipAddress);

        return await query.CountAsync();
    }
}
