namespace InShopDbModels.Models;

/// <summary>
/// Запись аудита смены статуса заказа (кто, когда, из какого статуса в какой).
/// </summary>
public class OrderAuditLog
{
    public long AuditId { get; set; }

    public int OrderId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    /// <summary>Корпоративный email администратора из JWT.</summary>
    public string ChangedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
