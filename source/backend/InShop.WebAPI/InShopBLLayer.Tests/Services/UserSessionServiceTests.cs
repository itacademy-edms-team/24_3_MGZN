using AutoMapper;
using Contracts.Dtos;
using FluentAssertions;
using InShopBLLayer.Services;
using InShopDbModels.Abstractions;
using InShopDbModels.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InShopBLLayer.Tests.Services;

public class UserSessionServiceTests
{
    private readonly Mock<IUserSessionRepository> _repository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UserSessionService _sut;

    public UserSessionServiceTests()
    {
        _sut = new UserSessionService(
            _repository.Object,
            _mapper.Object,
            NullLogger<UserSessionService>.Instance);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenTokenMissing_ReturnsInvalid()
    {
        _repository.Setup(r => r.GetSessionByTokenAsync(It.IsAny<Guid>()))
            .ReturnsAsync((UserSession?)null);

        var result = await _sut.ValidateSessionAsync(Guid.NewGuid());

        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Session not found");
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenSessionInactive_ReturnsInvalid()
    {
        var token = Guid.NewGuid();
        _repository.Setup(r => r.GetSessionByTokenAsync(token))
            .ReturnsAsync(new UserSession
            {
                SessionId = 1,
                IsActive = false,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });

        var result = await _sut.ValidateSessionAsync(token);

        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Session is inactive");
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenExpired_ReturnsInvalid()
    {
        var token = Guid.NewGuid();
        _repository.Setup(r => r.GetSessionByTokenAsync(token))
            .ReturnsAsync(new UserSession
            {
                SessionId = 1,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
            });

        var result = await _sut.ValidateSessionAsync(token);

        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Session expired");
    }
}
