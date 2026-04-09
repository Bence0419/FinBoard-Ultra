using FinBoardUltra.Domain.Enums;

namespace FinBoardUltra.Domain.Entities;

public class FinancialRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public RecordType Type { get; set; }

    /// <summary>Always positive. The Type field determines direction (in/out).</summary>
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }
    public Guid CategoryId { get; set; }
    public string? Note { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Populated only when Type == Investment.</summary>
    public InvestmentDetail? InvestmentDetail { get; set; }
}
