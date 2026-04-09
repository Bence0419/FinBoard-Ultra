using FinBoardUltra.Domain.Enums;

namespace FinBoardUltra.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }

    /// <summary>Null for system-default categories visible to all users.</summary>
    public Guid? UserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public RecordType Type { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDeleted { get; set; }
}
