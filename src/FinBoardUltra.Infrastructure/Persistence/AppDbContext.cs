using FinBoardUltra.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinBoardUltra.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<FinancialRecord> FinancialRecords => Set<FinancialRecord>();
    public DbSet<InvestmentDetail> InvestmentDetails => Set<InvestmentDetail>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MfaChallenge> MfaChallenges => Set<MfaChallenge>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Name).IsRequired();
            e.Property(u => u.PreferredCurrency).IsRequired().HasDefaultValue("USD");
        });

        // ── FinancialRecord ───────────────────────────────────────────────────
        modelBuilder.Entity<FinancialRecord>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,4)");
            e.Property(r => r.Type).IsRequired();
            e.Property(r => r.Date).IsRequired();
            e.HasOne(r => r.InvestmentDetail)
                .WithOne(d => d.Record)
                .HasForeignKey<InvestmentDetail>(d => d.RecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── InvestmentDetail ─────────────────────────────────────────────────
        modelBuilder.Entity<InvestmentDetail>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.RecordId).IsUnique();
            e.Property(d => d.Quantity).HasColumnType("decimal(18,4)");
            e.Property(d => d.PurchasePrice).HasColumnType("decimal(18,4)");
            e.Property(d => d.CurrentPrice).HasColumnType("decimal(18,4)");
            e.Property(d => d.AssetName).IsRequired();
        });

        // ── Category ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired();
            e.Property(c => c.Type).IsRequired();
        });

        // ── MfaChallenge ──────────────────────────────────────────────────────
        modelBuilder.Entity<MfaChallenge>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Code).IsRequired();
        });

        // ── UserSession ───────────────────────────────────────────────────────
        modelBuilder.Entity<UserSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Token).IsUnique();
            e.Property(s => s.Token).IsRequired();
            e.Property(s => s.RefreshToken).IsRequired();
        });

        // ── AuditLog ──────────────────────────────────────────────────────────
        // Append-only; no cascade deletes from any parent entity.
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).IsRequired();
            e.Property(a => a.Details).IsRequired();
            // UserId is intentionally not a FK — audit rows must survive user deletion.
        });
    }
}
