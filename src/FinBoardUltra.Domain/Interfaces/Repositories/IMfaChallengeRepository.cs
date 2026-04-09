using FinBoardUltra.Domain.Entities;

namespace FinBoardUltra.Domain.Interfaces.Repositories;

public interface IMfaChallengeRepository
{
    Task<MfaChallenge?> GetByIdAsync(Guid id);
    Task AddAsync(MfaChallenge challenge);

    /// <summary>Persists changes to an existing challenge (e.g. setting UsedAt).</summary>
    Task UpdateAsync(MfaChallenge challenge);
}
