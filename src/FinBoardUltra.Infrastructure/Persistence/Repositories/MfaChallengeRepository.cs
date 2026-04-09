using FinBoardUltra.Domain.Entities;
using FinBoardUltra.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinBoardUltra.Infrastructure.Persistence.Repositories;

public sealed class MfaChallengeRepository(AppDbContext db) : IMfaChallengeRepository
{
    public async Task<MfaChallenge?> GetByIdAsync(Guid id)
    {
        // No filtering on UsedAt or ExpiresAt — validity is enforced by AuthService.
        return await db.MfaChallenges.FindAsync(id);
    }

    public async Task AddAsync(MfaChallenge challenge)
    {
        if (challenge.Id == Guid.Empty) challenge.Id = Guid.NewGuid();
        if (challenge.CreatedAt == default) challenge.CreatedAt = DateTime.UtcNow;

        await db.MfaChallenges.AddAsync(challenge);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(MfaChallenge challenge)
    {
        db.MfaChallenges.Update(challenge);
        await db.SaveChangesAsync();
    }
}
