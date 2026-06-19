using FluentAssertions;
using InShopBLLayer.Services;
using Microsoft.EntityFrameworkCore;

namespace InShopBLLayer.Tests.Services;

public class ReviewServiceStaticTests
{
    [Theory]
    [InlineData("Violation of UNIQUE KEY constraint")]
    [InlineData("duplicate key value")]
    [InlineData("Error 2601")]
    [InlineData("Error 2627")]
    public void IsUniqueConstraintViolation_DetectsSqlServerUniqueErrors(string message)
    {
        var ex = new DbUpdateException(message);

        ReviewService.IsUniqueConstraintViolation(ex).Should().BeTrue();
    }

    [Fact]
    public void IsUniqueConstraintViolation_ReturnsFalse_ForOtherErrors()
    {
        var ex = new DbUpdateException("foreign key constraint failed");

        ReviewService.IsUniqueConstraintViolation(ex).Should().BeFalse();
    }
}
