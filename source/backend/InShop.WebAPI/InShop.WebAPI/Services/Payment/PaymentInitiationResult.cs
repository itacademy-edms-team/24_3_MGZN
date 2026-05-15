namespace InShop.WebAPI.Services.Payment
{
    /// <summary>
    /// Результат инициации оплаты — возвращается фронтенду из PaymentController.
    /// Для мока RedirectUrl пустой (оплата идёт через PaymentsAPI в фоне).
    /// Для ЮKassa RedirectUrl — ссылка на страницу оплаты ЮMoney.
    /// </summary>
    public class PaymentInitiationResult
    {
        public string? RedirectUrl { get; set; }
        public string? PaymentId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
