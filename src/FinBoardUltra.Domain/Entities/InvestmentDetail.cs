namespace FinBoardUltra.Domain.Entities;

/// <summary>
/// Extra fields for investment records. Stored in a separate table (1:1 with FinancialRecord).
/// Computed gain/loss = (CurrentPrice - PurchasePrice) * Quantity — never stored.
/// </summary>
public class InvestmentDetail
{
    public Guid Id { get; set; }
    public Guid RecordId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public string? Platform { get; set; }

    public FinancialRecord Record { get; set; } = null!;
}
