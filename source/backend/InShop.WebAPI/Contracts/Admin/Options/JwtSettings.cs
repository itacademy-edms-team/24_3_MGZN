namespace Contracts.Admin.Options
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = "InShop";
        public string Audience { get; set; } = "InShopAdmin";
        public string Key { get; set; } = null!;
        public int ExpirationHours { get; set; } = 8;
    }
}
