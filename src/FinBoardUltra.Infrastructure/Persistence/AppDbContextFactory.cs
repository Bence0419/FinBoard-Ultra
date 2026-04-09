using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinBoardUltra.Infrastructure.Persistence;

/// <summary>
/// Used exclusively by EF Core design-time tooling (dotnet ef migrations add / script).
/// Do NOT register this in the application DI container.
/// The real database path is supplied by the ConsoleApp at runtime.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=finboard-design.db")
            .Options;

        return new AppDbContext(options);
    }
}
