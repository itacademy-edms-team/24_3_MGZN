using InShopBLLayer.Abstractions;
using InShopBLLayer.Models;
using InShopBLLayer.Services.Admin;
using InShopDbModels.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RazorLight;

namespace InShopBLLayer.Services
{
    public class OrderStatusEmailNotifier : IOrderStatusEmailNotifier
    {
        private readonly IEmailSender _emailSender;
        private readonly OrderTrackingTokenService _trackingTokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderStatusEmailNotifier> _logger;
        private readonly Lazy<RazorLightEngine> _engine;

        public OrderStatusEmailNotifier(
            IEmailSender emailSender,
            OrderTrackingTokenService trackingTokenService,
            IConfiguration configuration,
            ILogger<OrderStatusEmailNotifier> logger)
        {
            _emailSender = emailSender;
            _trackingTokenService = trackingTokenService;
            _configuration = configuration;
            _logger = logger;
            _engine = new Lazy<RazorLightEngine>(CreateEngine);
        }

        public async Task SendOrderConfirmationAsync(Order order, CancellationToken ct = default)
        {
            var subject = "Подтверждение заказа";
            var headline = OrderStatusLabels.GetConfirmationHeadline(order.OrderId);
            await SendAsync(order, subject, headline, "OrderConfirmationTemplate.cshtml", ct);
        }

        public async Task SendStatusUpdateAsync(Order order, CancellationToken ct = default)
        {
            if (!OrderStatusLabels.ShouldSendStatusEmail(order.OrderStatus))
            {
                return;
            }

            var subject = OrderStatusLabels.GetStatusEmailSubject(order.OrderStatus);
            var headline = OrderStatusLabels.GetStatusEmailHeadline(order.OrderStatus, order.OrderId);
            await SendAsync(order, subject, headline, "OrderStatusUpdateTemplate.cshtml", ct);
        }

        private async Task SendAsync(
            Order order,
            string subject,
            string headline,
            string templateFileName,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                _logger.LogWarning("Заказ {OrderId}: email покупателя пуст, письмо не отправлено", order.OrderId);
                return;
            }

            try
            {
                var body = await RenderHtmlAsync(order, headline, templateFileName);
                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("Заказ {OrderId}: пустое тело письма", order.OrderId);
                    return;
                }

                await _emailSender.SendAsync(order.CustomerEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Заказ {OrderId}: не удалось отправить письмо «{Subject}»", order.OrderId, subject);
            }
        }

        private async Task<string> RenderHtmlAsync(Order order, string headline, string templateFileName)
        {
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";
            var trackingUrl = _trackingTokenService.BuildTrackingUrl(order.OrderId, frontendBaseUrl);

            var model = new OrderEmailTemplateModel
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate.ToString("dd.MM.yyyy"),
                StatusDisplayName = OrderStatusLabels.ToRussian(order.OrderStatus),
                OrderTotalAmount = order.OrderTotalAmount.ToString("C"),
                Headline = headline,
                TrackingUrl = trackingUrl,
                OrderItems = (order.OrderItems ?? Enumerable.Empty<OrderItem>())
                    .Select(item => new OrderItemTemplateModel
                    {
                        ProductName = item.Product?.ProductName ?? "Товар не найден",
                        QuantityItem = item.QuantityItem,
                        Price = item.Price.ToString("C"),
                        TotalPrice = (item.TotalPrice ?? item.Price * item.QuantityItem).ToString("C"),
                    })
                    .ToList()
            };

            return await _engine.Value.CompileRenderAsync(templateFileName, model);
        }

        private static RazorLightEngine CreateEngine()
        {
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates");
            return new RazorLightEngineBuilder()
                .UseFileSystemProject(templatePath)
                .UseMemoryCachingProvider()
                .Build();
        }
    }
}
