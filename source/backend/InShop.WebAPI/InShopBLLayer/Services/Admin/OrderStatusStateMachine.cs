using System.Collections.Frozen;

namespace InShopBLLayer.Services.Admin
{
    /// <summary>
    /// Конечный автомат статусов заказа для админ-панели.
    /// Покупательские потоки пишут legacy-строки (Unpayed, Payed) — нормализуем при валидации.
    /// </summary>
    public static class OrderStatusStateMachine
    {
        public const string Draft = "Draft";
        public const string Unpaid = "Unpaid";
        public const string Processing = "Processing";
        public const string Paid = "Paid";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";

        private static readonly FrozenDictionary<string, string> LegacyNormalization =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Unpayed"] = Unpaid,
                ["Payed"] = Paid,
                ["Formalization"] = Processing,
                ["WaitingPayment"] = Unpaid,
                ["Доставлен"] = Delivered,
                ["Завершен"] = Delivered,
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        private static readonly FrozenDictionary<string, HashSet<string>> AllowedTransitions =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [Draft] = new(StringComparer.OrdinalIgnoreCase) { Unpaid, Cancelled },
                [Unpaid] = new(StringComparer.OrdinalIgnoreCase) { Processing, Cancelled },
                [Processing] = new(StringComparer.OrdinalIgnoreCase) { Paid, Cancelled },
                [Paid] = new(StringComparer.OrdinalIgnoreCase) { Shipped, Cancelled },
                [Shipped] = new(StringComparer.OrdinalIgnoreCase) { Delivered, Cancelled },
                [Delivered] = new(StringComparer.OrdinalIgnoreCase),
                [Cancelled] = new(StringComparer.OrdinalIgnoreCase),
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        /// <summary>Приводит статус из БД к каноническому виду для FSM.</summary>
        public static string Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return Draft;
            }

            var trimmed = status.Trim();
            if (LegacyNormalization.TryGetValue(trimmed, out var mapped))
            {
                return mapped;
            }

            return AllowedTransitions.ContainsKey(trimmed) ? trimmed : trimmed;
        }

        public static bool CanTransition(string? currentStatus, string targetStatus)
        {
            var current = Normalize(currentStatus);
            var target = Normalize(targetStatus);

            if (string.Equals(current, Delivered, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(target, Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                return !string.Equals(current, Delivered, StringComparison.OrdinalIgnoreCase);
            }

            if (!AllowedTransitions.TryGetValue(current, out var allowed))
            {
                return false;
            }

            return allowed.Contains(target);
        }

        /// <summary>Финальные статусы: смена запрещена (в т.ч. из админки).</summary>
        public static bool IsTerminalStatus(string? status)
        {
            var current = Normalize(status);
            return string.Equals(current, Delivered, StringComparison.OrdinalIgnoreCase)
                || string.Equals(current, Cancelled, StringComparison.OrdinalIgnoreCase);
        }

        public static IReadOnlyList<string> GetAllowedNextStatuses(string? currentStatus)
        {
            var current = Normalize(currentStatus);

            if (string.Equals(current, Delivered, StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<string>();
            }

            var list = new List<string>();
            if (AllowedTransitions.TryGetValue(current, out var allowed))
            {
                list.AddRange(allowed);
            }

            if (!list.Contains(Cancelled, StringComparer.OrdinalIgnoreCase)
                && !string.Equals(current, Delivered, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(Cancelled);
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static void ValidateTransition(string? currentStatus, string targetStatus)
        {
            var target = Normalize(targetStatus);
            if (!AllowedTransitions.ContainsKey(target) && !string.Equals(target, Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Неизвестный целевой статус: {targetStatus}");
            }

            if (!CanTransition(currentStatus, target))
            {
                var current = Normalize(currentStatus);
                throw new InvalidOperationException(
                    $"Переход {current} → {target} запрещён. Допустимо: {string.Join(", ", GetAllowedNextStatuses(currentStatus))}");
            }
        }
    }
}
