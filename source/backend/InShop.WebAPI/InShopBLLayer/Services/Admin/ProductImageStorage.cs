using Microsoft.AspNetCore.Hosting;

namespace InShopBLLayer.Services.Admin
{
    /// <summary>
    /// Сохранение изображений товаров из Base64 в wwwroot/uploads/products/.
    /// </summary>
    public class ProductImageStorage
    {
        public const int MaxImageSizeBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly string _uploadRoot;

        public ProductImageStorage(IWebHostEnvironment environment)
        {
            _uploadRoot = Path.Combine(environment.WebRootPath ?? "wwwroot", "uploads", "products");
            Directory.CreateDirectory(_uploadRoot);
        }

        /// <summary>
        /// Декодирует Base64, проверяет размер и MIME по сигнатуре файла, сохраняет на диск.
        /// </summary>
        /// <returns>Относительный URL для Product.ImageUrl, например /uploads/products/abc.jpg</returns>
        public async Task<string> SaveBase64ImageAsync(string imageBase64, CancellationToken ct = default)
        {
            var (payload, declaredMime) = ParseBase64Payload(imageBase64);
            var bytes = Convert.FromBase64String(payload);

            if (bytes.Length > MaxImageSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Размер изображения {bytes.Length} байт превышает лимит {MaxImageSizeBytes} байт (5 МБ).");
            }

            var detectedMime = DetectMimeType(bytes) ?? declaredMime;
            if (detectedMime is null || !AllowedMimeTypes.Contains(detectedMime))
            {
                throw new InvalidOperationException("Допустимы только JPEG, PNG и WebP.");
            }

            var extension = detectedMime switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".bin"
            };

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(_uploadRoot, fileName);

            await File.WriteAllBytesAsync(fullPath, bytes, ct);

            return $"/uploads/products/{fileName}";
        }

        private static (string Base64Payload, string? Mime) ParseBase64Payload(string input)
        {
            var trimmed = input.Trim();
            if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var comma = trimmed.IndexOf(',');
                if (comma < 0)
                {
                    throw new InvalidOperationException("Некорректный формат data URL.");
                }

                var header = trimmed[..comma];
                var mime = header.Split(';')[0]["data:".Length..];
                return (trimmed[(comma + 1)..], mime);
            }

            return (trimmed, null);
        }

        private static string? DetectMimeType(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            {
                return "image/jpeg";
            }

            if (bytes.Length >= 8
                && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            {
                return "image/png";
            }

            if (bytes.Length >= 12
                && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
                && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            {
                return "image/webp";
            }

            return null;
        }
    }
}
