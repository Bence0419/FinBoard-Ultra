using FinBoardUltra.Domain.Entities;
using FinBoardUltra.Domain.Enums;

namespace FinBoardUltra.Domain.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id);

    /// <summary>
    /// Returns all non-deleted categories visible to the user: user-owned plus system defaults (UserId == null).
    /// Optionally filtered by record type.
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllAsync(Guid userId, RecordType? type = null);

    Task AddAsync(Category category);
    Task UpdateAsync(Category category);

    /// <summary>Used during first-run seeding to avoid duplicate default categories.</summary>
    Task<bool> AnyDefaultsExistAsync();
}
