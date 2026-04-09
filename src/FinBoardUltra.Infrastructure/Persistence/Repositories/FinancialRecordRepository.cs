using FinBoardUltra.Domain.Entities;
using FinBoardUltra.Domain.Enums;
using FinBoardUltra.Domain.Interfaces.Repositories;
using FinBoardUltra.Domain.Queries;
using Microsoft.EntityFrameworkCore;

namespace FinBoardUltra.Infrastructure.Persistence.Repositories;

public sealed class FinancialRecordRepository(AppDbContext db) : IFinancialRecordRepository
{
    public async Task<FinancialRecord?> GetByIdAsync(Guid userId, Guid recordId)
    {
        return await db.FinancialRecords
            .Include(r => r.InvestmentDetail)
            .FirstOrDefaultAsync(r => r.Id == recordId && r.UserId == userId && !r.IsDeleted);
    }

    public async Task<IReadOnlyList<FinancialRecord>> GetAllAsync(Guid userId, RecordFilter? filter = null)
    {
        var query = db.FinancialRecords
            .Include(r => r.InvestmentDetail)
            .Where(r => r.UserId == userId && !r.IsDeleted);

        if (filter is not null)
        {
            if (filter.DateFrom.HasValue)
                query = query.Where(r => r.Date >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(r => r.Date <= filter.DateTo.Value);

            if (filter.Type.HasValue)
                query = query.Where(r => r.Type == filter.Type.Value);

            if (filter.CategoryId.HasValue)
                query = query.Where(r => r.CategoryId == filter.CategoryId.Value);
        }

        return await query
            .OrderByDescending(r => r.Date)
            .ToListAsync();
    }

    public async Task AddAsync(FinancialRecord record)
    {
        if (record.Id == Guid.Empty) record.Id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        if (record.CreatedAt == default) record.CreatedAt = now;
        if (record.UpdatedAt == default) record.UpdatedAt = now;

        if (record.InvestmentDetail is not null && record.InvestmentDetail.Id == Guid.Empty)
            record.InvestmentDetail.Id = Guid.NewGuid();

        await db.FinancialRecords.AddAsync(record);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(FinancialRecord record)
    {
        record.UpdatedAt = DateTime.UtcNow;
        db.FinancialRecords.Update(record);
        await db.SaveChangesAsync();
    }

    public async Task<decimal> SumByTypeAsync(Guid userId, RecordType type, DateOnly? from, DateOnly? to)
    {
        var query = db.FinancialRecords
            .Where(r => r.UserId == userId && r.Type == type && !r.IsDeleted);

        if (from.HasValue) query = query.Where(r => r.Date >= from.Value);
        if (to.HasValue)   query = query.Where(r => r.Date <= to.Value);

        return await query.SumAsync(r => (decimal?)r.Amount) ?? 0m;
    }

    public async Task<IReadOnlyList<FinancialRecord>> GetRecentAsync(Guid userId, int count)
    {
        return await db.FinancialRecords
            .Include(r => r.InvestmentDetail)
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.Date)
            .Take(count)
            .ToListAsync();
    }
}
