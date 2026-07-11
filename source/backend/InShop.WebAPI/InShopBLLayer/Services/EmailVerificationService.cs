// InShopBLLayer.Services/EmailVerificationService.cs
using InShopBLLayer.Abstractions;
using InShopBLLayer.BLModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private const int MaxValidationAttempts = 5;

        private readonly IMemoryCache _cache;
        private readonly IEmailSender _emailSender;

        public EmailVerificationService(IMemoryCache cache, IEmailSender emailSender)
        {
            _cache = cache;
            _emailSender = emailSender;
        }

        public async Task<string> GenerateAndSendCodeAsync(string email)
        {
            email = NormalizeEmail(email);
            var code = GenerateRandomCode();
            var verificationCode = new EmailVerificationCode(email, code, TimeSpan.FromMinutes(5));

            _cache.Set(email, verificationCode, TimeSpan.FromMinutes(5));
            _cache.Remove(GetAttemptsKey(email));

            await _emailSender.SendAsync(email, "Ваш код подтверждения", $"Ваш код: {code}");

            return code;
        }

        public bool ValidateCode(string email, string code)
        {
            email = NormalizeEmail(email);
            var attemptsKey = GetAttemptsKey(email);
            var attempts = _cache.TryGetValue(attemptsKey, out int currentAttempts)
                ? currentAttempts
                : 0;

            if (attempts >= MaxValidationAttempts)
                return false;

            if (!_cache.TryGetValue(email, out EmailVerificationCode? storedCode) || storedCode is null)
                return false;

            if (!storedCode.IsValid || storedCode.Code != code)
            {
                _cache.Set(attemptsKey, attempts + 1, TimeSpan.FromMinutes(5));
                return false;
            }

            storedCode.IsUsed = true;
            _cache.Remove(email);
            _cache.Remove(attemptsKey);

            return true;
        }

        private string GenerateRandomCode()
        {
            var number = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000);
            return number.ToString("D6");
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string GetAttemptsKey(string email)
        {
            return $"email-verification-attempts:{email}";
        }
    }
}