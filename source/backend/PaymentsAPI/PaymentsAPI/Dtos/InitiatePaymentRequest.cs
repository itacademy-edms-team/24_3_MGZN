namespace PaymentsAPI.Dtos
{
    public class InitiatePaymentRequest
    {
        public int OrderId { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public string CardholderName { get; set; } = string.Empty;
    }
}
