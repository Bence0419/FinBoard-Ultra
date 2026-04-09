using FinBoardUltra.Domain.Entities;
using FinBoardUltra.Domain.Enums;
using FinBoardUltra.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinBoardUltra.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await db.Categories
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(Guid userId, RecordType? type = null)
    {
        var query = db.Categories
            .Where(c => !c.IsDeleted && (c.UserId == userId || c.UserId == null));

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        return await query.ToListAsync();
    }

    public async Task AddAsync(Category category)
    {
        if (category.Id == Guid.Empty) category.Id = Guid.NewGuid();

        await db.Categories.AddAsync(category);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        db.Categories.Update(category);
        await db.SaveChangesAsync();
    }

    public async Task<bool> AnyDefaultsExistAsync()
    {
        // Intentionally does NOT filter on IsDeleted — checks for the physical existence
        // of seed data so the seeder never re-runs after a soft-delete.
        return await db.Categories.AnyAsync(c => c.IsDefault);
    }
}
