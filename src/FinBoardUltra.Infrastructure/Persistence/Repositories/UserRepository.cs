using FinBoardUltra.Domain.Entities;
using FinBoardUltra.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinBoardUltra.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await db.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        // EF Core translates ToLower() to LOWER() in SQLite for case-insensitive matching.
        var normalised = email.ToLowerInvariant();
        return await db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalised && !u.IsDeleted);
    }

    public async Task AddAsync(User user)
    {
        if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
        if (user.CreatedAt == default) user.CreatedAt = DateTime.UtcNow;

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }
}
