using AutoMapper;
using Contracts.Dtos;
using FluentAssertions;
using InShop.IntegrationTests.Infrastructure;
using InShopBLLayer.Abstractions;
using InShopBLLayer.MappingProfiles;
using InShopBLLayer.Services;
using InShopDbModels.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace InShop.IntegrationTests.Services;

[Collection("SqlServer")]
public class ReviewServiceIntegrationTests
{
    private readonly SqlServerFixture _fixture;

    public ReviewServiceIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddReviewAsync_WhenDuplicateFromSameSession_Throws()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var session = await TestDataSeeder.SeedSessionAsync(context);
        var product = await TestDataSeeder.SeedProductAsync(context);
        var sut = CreateReviewService(context);

        await sut.AddReviewAsync(product.ProductId, session.SessionId, new CreateReviewDto
        {
            Rating = 5,
            Comment = "Отличный товар, рекомендую!"
        });

        var act = () => sut.AddReviewAsync(product.ProductId, session.SessionId, new CreateReviewDto
        {
            Rating = 4,
            Comment = "Повторный отзыв не должен пройти"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*уже оставили отзыв*");
    }

    [Fact]
    public async Task VoteReviewAsync_TogglesVote_WhenSameTypeVotedTwice()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var session = await TestDataSeeder.SeedSessionAsync(context);
        var product = await TestDataSeeder.SeedProductAsync(context);
        var sut = CreateReviewService(context);

        var review = await sut.AddReviewAsync(product.ProductId, session.SessionId, new CreateReviewDto
        {
            Rating = 5,
            Comment = "Первый отзыв для голосования"
        });

        await sut.VoteReviewAsync(review.ReviewId, session.SessionId, voteType: 1);
        await sut.VoteReviewAsync(review.ReviewId, session.SessionId, voteType: 1);

        var votes = context.ReviewVotes.Where(v => v.ReviewId == review.ReviewId).ToList();
        votes.Should().BeEmpty();
    }

    [Fact]
    public async Task VoteReviewAsync_WithInvalidVoteType_Throws()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var sut = CreateReviewService(context);

        var act = () => sut.VoteReviewAsync(reviewId: 1, sessionId: 1, voteType: 2);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static ReviewService CreateReviewService(InShopDbModels.Data.AppDbContext context)
    {
        var mapper = TestMapperFactory.CreateReviewMapper();
        var cacheMock = new Mock<IReviewCacheService>();

        return new ReviewService(
            context,
            new ProductReviewRepository(context),
            new ReviewVoteRepository(context),
            new ProductRepository(context),
            new OrderItemRepository(context),
            cacheMock.Object,
            mapper);
    }
}
