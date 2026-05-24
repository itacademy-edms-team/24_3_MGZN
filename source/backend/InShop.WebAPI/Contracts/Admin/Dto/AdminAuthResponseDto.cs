namespace Contracts.Admin.Dto
{
    public class AdminAuthResponseDto
    {
        public string Token { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
