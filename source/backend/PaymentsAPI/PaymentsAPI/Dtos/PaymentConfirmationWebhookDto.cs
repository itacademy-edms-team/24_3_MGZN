namespace PaymentsAPI.Dtos
{
    public class PaymentConfirmationWebhookDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
