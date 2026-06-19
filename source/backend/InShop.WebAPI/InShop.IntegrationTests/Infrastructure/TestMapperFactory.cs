using AutoMapper;
using InShopBLLayer.MappingProfiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InShop.IntegrationTests.Infrastructure;

internal static class TestMapperFactory
{
    public static IMapper CreateAdminMapper() => CreateMapper<AdminProfile>();

    public static IMapper CreateReviewMapper() => CreateMapper<ReviewProfile>();

    private static IMapper CreateMapper<TProfile>() where TProfile : Profile, new()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<TProfile>());
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
