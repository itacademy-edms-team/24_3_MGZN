using FluentAssertions;
using InShopBLLayer.Abstractions;
using InShopBLLayer.BLModels;
using InShopBLLayer.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace InShopBLLayer.Tests.Services;

public class EmailVerificationServiceTests
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly EmailVerificationService _sut;

    public EmailVerificationServiceTests()
    {
        _sut = new EmailVerificationService(_cache, _emailSender.Object);
    }

    [Fact]
    public async Task GenerateAndSendCodeAsync_StoresCodeInCacheAndSendsEmail()
    {
        const string email = "user@example.com";

        var code = await _sut.GenerateAndSendCodeAsync(email);

        code.Should().MatchRegex(@"^\d{4}$");
        _cache.TryGetValue(email, out EmailVerificationCode stored).Should().BeTrue();
        stored!.Code.Should().Be(code);
        stored.IsValid.Should().BeTrue();

        _emailSender.Verify(s => s.SendAsync(
            email,
            "Ваш код подтверждения",
            It.Is<string>(body => body.Contains(code))), Times.Once);
    }

    [Fact]
    public void ValidateCode_WhenCodeMatches_ReturnsTrueAndMarksUsed()
    {
        const string email = "user@example.com";
        var stored = new EmailVerificationCode(email, "1234", TimeSpan.FromMinutes(5));
        _cache.Set(email, stored);

        var result = _sut.ValidateCode(email, "1234");

        result.Should().BeTrue();
        _cache.TryGetValue(email, out EmailVerificationCode updated).Should().BeTrue();
        updated!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_WhenWrongCode_ReturnsFalse()
    {
        const string email = "user@example.com";
        _cache.Set(email, new EmailVerificationCode(email, "1234", TimeSpan.FromMinutes(5)));

        var result = _sut.ValidateCode(email, "9999");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCode_WhenEmailNotInCache_ReturnsFalse()
    {
        var result = _sut.ValidateCode("missing@example.com", "1234");

        result.Should().BeFalse();
    }
}
