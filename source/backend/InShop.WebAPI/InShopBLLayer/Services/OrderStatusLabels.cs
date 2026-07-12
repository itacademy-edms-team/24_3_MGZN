namespace InShopBLLayer.Services.Admin
{
    /// <summary>Русские подписи статусов и метаданные писем о смене статуса.</summary>
    public static class OrderStatusLabels
    {
        public static string ToRussian(string? status)
        {
            return OrderStatusStateMachine.Normalize(status) switch
            {
                OrderStatusStateMachine.Draft => "Черновик",
                OrderStatusStateMachine.Unpaid => "Ожидает оплаты",
                OrderStatusStateMachine.Processing => "В обработке",
                OrderStatusStateMachine.Paid => "Оплачен",
                OrderStatusStateMachine.Shipped => "Отправлен",
                OrderStatusStateMachine.Delivered => "Доставлен",
                OrderStatusStateMachine.Cancelled => "Отменён",
                _ => string.IsNullOrWhiteSpace(status) ? "Неизвестно" : status.Trim()
            };
        }

        /// <summary>Статусы, по которым шлём письмо об обновлении (не confirmation).</summary>
        public static bool ShouldSendStatusEmail(string? status)
        {
            var canonical = OrderStatusStateMachine.Normalize(status);
            return canonical is OrderStatusStateMachine.Paid
                or OrderStatusStateMachine.Shipped
                or OrderStatusStateMachine.Delivered
                or OrderStatusStateMachine.Cancelled;
        }

        public static string GetStatusEmailSubject(string? status)
        {
            return OrderStatusStateMachine.Normalize(status) switch
            {
                OrderStatusStateMachine.Paid => "Заказ оплачен",
                OrderStatusStateMachine.Shipped => "Заказ отправлен",
                OrderStatusStateMachine.Delivered => "Заказ доставлен",
                OrderStatusStateMachine.Cancelled => "Заказ отменён",
                _ => "Обновление заказа"
            };
        }

        public static string GetStatusEmailHeadline(string? status, int orderId)
        {
            return OrderStatusStateMachine.Normalize(status) switch
            {
                OrderStatusStateMachine.Paid => $"Заказ #{orderId} оплачен!",
                OrderStatusStateMachine.Shipped => $"Заказ #{orderId} отправлен!",
                OrderStatusStateMachine.Delivered => $"Заказ #{orderId} доставлен!",
                OrderStatusStateMachine.Cancelled => $"Заказ #{orderId} отменён",
                _ => $"Обновление заказа #{orderId}"
            };
        }

        public static string GetConfirmationHeadline(int orderId) =>
            $"Ваш заказ #{orderId} подтверждён!";
    }
}
