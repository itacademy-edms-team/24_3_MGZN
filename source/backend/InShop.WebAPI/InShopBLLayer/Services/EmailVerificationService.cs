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
        private readonly IMemoryCache _cache;
        private readonly IEmailSender _emailSender;

        public EmailVerificationService(IMemoryCache cache, IEmailSender emailSender)
        {
            _cache = cache;
            _emailSender = emailSender;
        }

        public async Task<string> GenerateAndSendCodeAsync(string email)
        {
            var code = GenerateRandomCode();
            var verificationCode = new EmailVerificationCode(email, code, TimeSpan.FromMinutes(5));

            _cache.Set(email, verificationCode, TimeSpan.FromMinutes(5));

            await _emailSender.SendAsync(email, "Ваш код подтверждения", $"Ваш код: {code}");

            return code;
        }

        public bool ValidateCode(string email, string code)
        {
            if (!_cache.TryGetValue(email, out EmailVerificationCode storedCode))
                return false;

            if (!storedCode.IsValid || storedCode.Code != code)
                return false;

            storedCode.IsUsed = true;
            _cache.Set(email, storedCode, TimeSpan.FromMinutes(5));

            return true;
        }

        private string GenerateRandomCode()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] bytes = new byte[2];
            rng.GetBytes(bytes);
            int number = BitConverter.ToUInt16(bytes, 0) % 10000;
            return number.ToString("D4");
        }
    }
}