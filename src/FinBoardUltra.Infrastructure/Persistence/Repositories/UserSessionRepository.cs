using FinBoardUltra.Domain.Entities;
using FinBoardUltra.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinBoardUltra.Infrastructure.Persistence.Repositories;

public sealed class UserSessionRepository(AppDbContext db) : IUserSessionRepository
{
    public async Task<UserSession?> GetByTokenAsync(string token)
    {
        // No filtering on IsRevoked or ExpiresAt — validity is enforced by AuthService.
        return await db.UserSessions
            .FirstOrDefaultAsync(s => s.Token == token);
    }

    public async Task AddAsync(UserSession session)
    {
        if (session.Id == Guid.Empty) session.Id = Guid.NewGuid();
        if (session.CreatedAt == default) session.CreatedAt = DateTime.UtcNow;

        await db.UserSessions.AddAsync(session);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserSession session)
    {
        db.UserSessions.Update(session);
        await db.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        // Bulk update — avoids loading every session into memory.
        await db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRevoked, true));
    }
}
