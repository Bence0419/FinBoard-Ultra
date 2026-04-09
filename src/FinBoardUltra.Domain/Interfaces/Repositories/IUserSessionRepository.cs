using FinBoardUltra.Domain.Entities;

namespace FinBoardUltra.Domain.Interfaces.Repositories;

public interface IUserSessionRepository
{
    Task<UserSession?> GetByTokenAsync(string token);
    Task AddAsync(UserSession session);

    /// <summary>Persists changes to an existing session (e.g. setting IsRevoked).</summary>
    Task UpdateAsync(UserSession session);

    /// <summary>Revokes all active sessions for a user (called on password change).</summary>
    Task RevokeAllForUserAsync(Guid userId);
}
