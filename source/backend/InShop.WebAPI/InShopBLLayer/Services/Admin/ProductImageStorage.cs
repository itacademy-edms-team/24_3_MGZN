using Microsoft.AspNetCore.Hosting;

namespace InShopBLLayer.Services.Admin
{
    /// <summary>
    /// Сохранение изображений из Base64 в wwwroot/uploads/{subFolder}/.
    /// </summary>
    public class ProductImageStorage
    {
        public const int MaxImageSizeBytes = 5 * 1024 * 1024;
        public const string ProductsSubFolder = "products";
        public const string CategoriesSubFolder = "categories";

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly string _webRoot;

        public ProductImageStorage(IWebHostEnvironment environment)
        {
            _webRoot = environment.WebRootPath ?? "wwwroot";
            Directory.CreateDirectory(GetUploadRoot(ProductsSubFolder));
            Directory.CreateDirectory(GetUploadRoot(CategoriesSubFolder));
        }

        /// <summary>
        /// Декодирует Base64, проверяет размер и MIME по сигнатуре файла, сохраняет на диск.
        /// </summary>
        /// <returns>Относительный URL, например /uploads/products/abc.jpg</returns>
        public Task<string> SaveBase64ImageAsync(string imageBase64, CancellationToken ct = default)
            => SaveBase64ImageAsync(imageBase64, ProductsSubFolder, ct);

        public async Task<string> SaveBase64ImageAsync(string imageBase64, string subFolder, CancellationToken ct = default)
        {
            var safeFolder = NormalizeSubFolder(subFolder);
            var uploadRoot = GetUploadRoot(safeFolder);
            Directory.CreateDirectory(uploadRoot);

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
            var fullPath = Path.Combine(uploadRoot, fileName);

            await File.WriteAllBytesAsync(fullPath, bytes, ct);

            return $"/uploads/{safeFolder}/{fileName}";
        }

        /// <summary>
        /// Удаляет файл с диска, если URL указывает на наш каталог uploads/products.
        /// Внешние URL не трогаем.
        /// </summary>
        public bool TryDeleteProductImageFile(string? imageUrl)
            => TryDeleteImageFile(imageUrl, ProductsSubFolder);

        public bool TryDeleteImageFile(string? imageUrl, string subFolder)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return false;
            }

            var safeFolder = NormalizeSubFolder(subFolder);
            var prefix = $"/uploads/{safeFolder}/";
            if (!imageUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileName = Path.GetFileName(imageUrl);
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            var fullPath = Path.Combine(GetUploadRoot(safeFolder), fileName);
            if (!File.Exists(fullPath))
            {
                return false;
            }

            File.Delete(fullPath);
            return true;
        }

        private string GetUploadRoot(string subFolder)
            => Path.Combine(_webRoot, "uploads", subFolder);

        private static string NormalizeSubFolder(string subFolder)
        {
            if (string.IsNullOrWhiteSpace(subFolder))
            {
                throw new ArgumentException("Подкаталог загрузки обязателен.", nameof(subFolder));
            }

            var trimmed = subFolder.Trim().Trim('/', '\\');
            if (trimmed.Contains("..", StringComparison.Ordinal)
                || trimmed.Contains('/', StringComparison.Ordinal)
                || trimmed.Contains('\\', StringComparison.Ordinal))
            {
                throw new ArgumentException("Некорректный подкаталог загрузки.", nameof(subFolder));
            }

            return trimmed.ToLowerInvariant();
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
