using FinBoardUltra.Domain.Enums;

namespace FinBoardUltra.Domain.Queries;

/// <summary>
/// Optional filter parameters passed to IFinancialRecordRepository queries.
/// All fields are optional and combined with AND logic when set.
/// </summary>
public sealed class RecordFilter
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public RecordType? Type { get; init; }
    public Guid? CategoryId { get; init; }
}
