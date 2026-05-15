namespace Contracts.Dtos
{
    /// <summary>
    /// Запрос на инициацию оплаты (ЮKassa) — достаточно orderId, карта не нужна.
    /// </summary>
    public class InitiatePaymentRequestDto
    {
        public int OrderId { get; set; }
    }
}
