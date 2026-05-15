namespace Contracts.Dtos
{
    /// <summary>
    /// Запрос синхронизации статуса после возврата с страницы ЮKassa (paymentId берётся из БД).
    /// </summary>
    public class ConfirmYooKassaPaymentRequestDto
    {
        public int OrderId { get; set; }
    }
}
