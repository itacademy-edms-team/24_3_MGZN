using InShopDbModels.Data;
using InShopDbModels.Models;

namespace InShop.IntegrationTests.Infrastructure;

internal static class TestDataSeeder
{
    public static async Task<Product> SeedProductAsync(
        AppDbContext context,
        int stock = 10,
        int reserved = 0)
    {
        var category = new Category { CategoryName = $"Cat-{Guid.NewGuid():N}" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            ProductName = "Test product",
            ProductPrice = 999m,
            ProductAvailability = true,
            ProductCategoryId = category.CategoryId,
            ProductStockQuantity = stock,
            ReservedQuantity = reserved,
            AverageRating = 0,
            ReviewsCount = 0
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    public static async Task<UserSession> SeedSessionAsync(AppDbContext context)
    {
        var session = new UserSession
        {
            UserIpaddress = "127.0.0.1",
            CreatedAt = DateTime.UtcNow,
            SessionToken = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            UserAgent = "integration-test"
        };

        context.UserSessions.Add(session);
        await context.SaveChangesAsync();
        return session;
    }

    public static async Task<Order> SeedOrderAsync(
        AppDbContext context,
        int sessionId,
        string status = "Unpaid")
    {
        var order = new Order
        {
            OrderStatus = status,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ShipMethod = "courier",
            PayStatus = "Unpayed",
            CustomerFullname = "Test User",
            PayMethod = "card",
            CustomerEmail = "test@example.com",
            CustomerPhoneNumber = "+70000000000",
            OrderTotalAmount = 0m,
            SessionId = sessionId
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    public static async Task<(Category Category, Product Product)> SeedCatalogAsync(
        AppDbContext context,
        string categoryName = "Ноутбуки",
        string productName = "Test Laptop",
        decimal price = 99999m,
        int stock = 10)
    {
        var category = new Category { CategoryName = categoryName };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            ProductName = productName,
            ProductPrice = price,
            ProductAvailability = true,
            ProductCategoryId = category.CategoryId,
            ProductStockQuantity = stock,
            ReservedQuantity = 0,
            AverageRating = 0,
            ReviewsCount = 0
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();
        return (category, product);
    }

    public static async Task<ShipCompany> SeedShipCompanyAsync(
        AppDbContext context,
        string name = "CDEK",
        string contact = "support@cdek.ru")
    {
        var company = new ShipCompany
        {
            ShipCompanyName = name,
            Contact = contact
        };

        context.ShipCompanies.Add(company);
        await context.SaveChangesAsync();
        return company;
    }
}
