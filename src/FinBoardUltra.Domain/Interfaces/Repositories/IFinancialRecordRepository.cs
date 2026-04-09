using FinBoardUltra.Domain.Entities;
using FinBoardUltra.Domain.Enums;
using FinBoardUltra.Domain.Queries;

namespace FinBoardUltra.Domain.Interfaces.Repositories;

public interface IFinancialRecordRepository
{
    Task<FinancialRecord?> GetByIdAsync(Guid userId, Guid recordId);
    Task<IReadOnlyList<FinancialRecord>> GetAllAsync(Guid userId, RecordFilter? filter = null);
    Task AddAsync(FinancialRecord record);
    Task UpdateAsync(FinancialRecord record);

    /// <summary>Returns the sum of amounts for the given type, optionally within a date range.</summary>
    Task<decimal> SumByTypeAsync(Guid userId, RecordType type, DateOnly? from, DateOnly? to);

    /// <summary>Returns the most recent <paramref name="count"/> non-deleted records, ordered by Date descending.</summary>
    Task<IReadOnlyList<FinancialRecord>> GetRecentAsync(Guid userId, int count);
}
