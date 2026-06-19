using System.Security.Claims;
using Contracts.Admin.Options;
using FluentAssertions;
using InShopBLLayer.Services.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace InShopBLLayer.Tests.Services.Admin;

public class AdminAuthServiceTests
{
    private readonly Mock<UserManager<IdentityUser>> _userManager;
    private readonly Mock<SignInManager<IdentityUser>> _signInManager;
    private readonly AdminAuthService _sut;

    public AdminAuthServiceTests()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        _userManager = new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _signInManager = new Mock<SignInManager<IdentityUser>>(
            _userManager.Object,
            Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            Mock.Of<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<IdentityUser>>(),
            null!, null!, null!, null!);

        var jwt = new JwtSettings
        {
            Key = "test-key-at-least-32-characters-long!!",
            Issuer = "InShop.Tests",
            Audience = "InShop.Tests",
            ExpirationHours = 1
        };

        _sut = new AdminAuthService(
            _userManager.Object,
            _signInManager.Object,
            Options.Create(jwt),
            NullLogger<AdminAuthService>.Instance);
    }

    [Fact]
    public void GetMe_ReturnsEmailAndRolesFromClaims()
    {
        var identity = new ClaimsIdentity("Bearer");
        identity.AddClaim(new Claim(ClaimTypes.Email, "admin@test.com"));
        identity.AddClaim(new Claim(ClaimTypes.Role, AdminAuthService.AdminRoleName));
        var principal = new ClaimsPrincipal(identity);

        var result = _sut.GetMe(principal);

        result.Email.Should().Be("admin@test.com");
        result.Roles.Should().Contain(AdminAuthService.AdminRoleName);
    }

    [Fact]
    public void GetMe_WhenEmailOnlyInJwtClaim_ReturnsEmail()
    {
        var identity = new ClaimsIdentity("Bearer");
        identity.AddClaim(new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, "jwt@test.com"));
        var principal = new ClaimsPrincipal(identity);

        var result = _sut.GetMe(principal);

        result.Email.Should().Be("jwt@test.com");
    }
}
