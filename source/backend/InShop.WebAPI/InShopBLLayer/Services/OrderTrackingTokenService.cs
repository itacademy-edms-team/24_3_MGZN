using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace InShopBLLayer.Services
{
    /// <summary>
    /// HMAC-токен для публичной ссылки отслеживания заказа (без колонки в БД).
    /// </summary>
    public class OrderTrackingTokenService
    {
        private readonly byte[] _key;

        public OrderTrackingTokenService(IConfiguration configuration)
        {
            var secret = configuration["OrderTracking:Secret"]
                ?? configuration["Frontend:TrackingSecret"]
                ?? "InShop-Dev-OrderTracking-Secret-ChangeMe";
            _key = Encoding.UTF8.GetBytes(secret);
        }

        public string CreateToken(int orderId)
        {
            var payload = orderId.ToString(CultureInfo.InvariantCulture);
            using var hmac = new HMACSHA256(_key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Base64UrlEncode(hash);
        }

        public bool ValidateToken(int orderId, string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var expected = CreateToken(orderId);
            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            var actualBytes = Encoding.UTF8.GetBytes(token.Trim());
            return expectedBytes.Length == actualBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }

        public string BuildTrackingUrl(int orderId, string frontendBaseUrl)
        {
            var baseUrl = string.IsNullOrWhiteSpace(frontendBaseUrl)
                ? "http://localhost:3000"
                : frontendBaseUrl.TrimEnd('/');
            var token = CreateToken(orderId);
            return $"{baseUrl}/order-track/{orderId}?t={Uri.EscapeDataString(token)}";
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
