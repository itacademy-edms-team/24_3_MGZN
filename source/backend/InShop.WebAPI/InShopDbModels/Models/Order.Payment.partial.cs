namespace InShopDbModels.Models;

/// <summary>
/// Расширение сущности Order под ЮKassa (DB-first).
/// Колонка добавляется в SQL Server скриптом init/add_yookassa_payment_id.sql.
/// При полном reverse scaffold таблицы Orders это свойство появится в Order.cs —
/// тогда этот файл можно удалить, чтобы не было дубликата.
/// </summary>
public partial class Order
{
    public string? YooKassaPaymentId { get; set; }
}
