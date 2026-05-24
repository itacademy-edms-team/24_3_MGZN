# Database-first (scaffold) + Identity в InShop

## Идея

| Слой | Контекст | Откуда в БД | Reverse scaffold |
|------|----------|-------------|------------------|
| Магазин (заказы, товары, сессии) | `AppDbContext` | Ваши таблицы | **Да**, с фильтром таблиц |
| Админ JWT | `AdminIdentityDbContext` | `AspNet*` | **Нет**, только SQL |
| Доп. поля / аудит | `AppDbContext.NonScaffolded.partial.cs` | SQL-скрипты | **Нет** |

Два контекста — **одна** база `InShopDB`, одна строка подключения.

## Шаг 1. SQL в базе (один раз)

В SSMS на `InShopDB`, **в таком порядке**:

1. `scripts/CreateAspNetIdentityTables.sql` — `AspNetUsers`, `AspNetRoles`, …
2. `scripts/AddAdminIdentityAndAudit.sql` — `OrderAuditLogs`, `ImageURL` 500
3. `scripts/AddProductReservationColumns.sql` — `ReservedQuantity`, `RowVersion` (если ещё не делали)

Проверка:

```sql
SELECT name FROM sys.tables WHERE name LIKE 'AspNet%' ORDER BY name;
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrderAuditLogs';
```

## Шаг 2. Код (уже в репозитории)

- `AdminIdentityDbContext.cs` — только Identity
- `AppDbContext.cs` — бизнес (после scaffold перезаписывается)
- `AppDbContext.NonScaffolded.partial.cs` — **не трогать scaffold’ом**: аудит, резерв, ЮKassa

`AddEntityFrameworkStores<AdminIdentityDbContext>()` в `AdminIdentityExtensions.cs`.

## Шаг 3. Reverse scaffold (когда меняется схема магазина)

Из папки `InShop.WebAPI` (startup — WebAPI, нужен пакет `Microsoft.EntityFrameworkCore.Design`):

```powershell
cd source/backend/InShop.WebAPI/InShop.WebAPI

# Пример: перегенерировать только бизнес-таблицы (БЕЗ AspNet*)
dotnet ef dbcontext scaffold `
  "Server=ВАШ_СЕРВЕР;Database=InShopDB;Integrated Security=True;TrustServerCertificate=True" `
  Microsoft.EntityFrameworkCore.SqlServer `
  --project ..\InShopDbModels\InShopDbModels.csproj `
  --startup-project . `
  --context AppDbContext `
  --context-dir Data `
  --output-dir Models `
  --namespace InShopDbModels.Models `
  --context-namespace InShopDbModels.Data `
  --table Admins `
  --table Categories `
  --table Orders `
  --table Order_Items `
  --table Products `
  --table ProductReviews `
  --table ProductSpecGroups `
  --table ProductSpecLinks `
  --table ProductSpecValues `
  --table ProductSpecifications `
  --table ReviewVotes `
  --table Ship_Companies `
  --table UserSession `
  --force
```

**Не указывайте** `--table AspNetUsers` и другие `AspNet*`.

После scaffold:

1. Убедитесь, что `AppDbContext` наследует `DbContext`, а не `IdentityDbContext`.
2. В конце `OnModelCreating` остаётся вызов `OnModelCreatingPartial(modelBuilder);`
3. Не удаляйте `AppDbContext.NonScaffolded.partial.cs` и `AdminIdentityDbContext.cs`
4. Поля `Product.ReservedQuantity` / `RowVersion` в scaffold могут пропасть из `Product.cs` — верните в `Models/Product.cs` вручную или добавьте `partial class Product` в отдельном файле

## Шаг 4. Первый админ

1. Запустить API
2. Swagger → `POST /api/Admin/auth/register` с корпоративным email
3. Повторный register → 409
4. Удалить эндпоинт register перед production

## Частые ошибки

| Ошибка | Причина |
|--------|---------|
| `Invalid object name 'AspNetUsers'` | Не выполнен `CreateAspNetIdentityTables.sql` |
| Scaffold затёр Identity | Scaffold делали на `AppDbContext : IdentityDbContext` или включили AspNet в --table |
| Дублирование `OnModelCreatingPartial` | Несколько partial с одним методом — только в `NonScaffolded.partial.cs` |
| Старая таблица `Admins` | Legacy, не используется для JWT; логин только через `AspNetUsers` |

## Альтернатива: сгенерировать SQL из EF (без применения к БД)

Если установлен `dotnet-ef` и `Microsoft.EntityFrameworkCore.Design` в startup-проекте:

```powershell
dotnet ef migrations add InitialAdminIdentity --context AdminIdentityDbContext --project ..\InShopDbModels --startup-project . --output-dir Migrations\AdminIdentity
dotnet ef migrations script --idempotent -o ..\scripts\CreateAspNetIdentityTables_generated.sql --context AdminIdentityDbContext
```

Сравните с `CreateAspNetIdentityTables.sql` и примените один вариант в SSMS.
