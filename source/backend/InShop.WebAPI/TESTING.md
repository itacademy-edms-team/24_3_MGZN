# Тестирование InShop (backend)

Документ описывает структуру автотестов, стек и порядок запуска.

## Структура проектов

| Проект | Тип | Назначение |
|--------|-----|------------|
| `InShopBLLayer.Tests` | Unit | Бизнес-логика без БД; репозитории и `IMapper` — моки (Moq) |
| `InShop.IntegrationTests` | Integration + API | Реальный SQL Server в Docker (Testcontainers) |
| `cypress-tests/` (frontend) | E2E | Сценарии в браузере; запуск локально |

**PaymentsAPI** в покрытие не входит (по решению команды).

## Стек тестирования

### Backend unit

- **xUnit** — фреймворк тестов (`[Fact]`, `[Theory]`)
- **FluentAssertions** — читаемые проверки (`result.Should().Be(...)`)
- **Moq** — заглушки интерфейсов (`IOrderRepository`, `IMapper`, …)
- **Coverlet** — сбор code coverage при `dotnet test`

### Backend integration

- **Testcontainers.MsSql** — поднимает контейнер `mcr.microsoft.com/mssql/server:2022-latest` на время прогона
- **EF Core `EnsureCreated`** — создаёт схему БД из `AppDbContext` + `AdminIdentityDbContext`
- **AutoMapper** — реальные профили (`AdminProfile`, `ReviewProfile`) через `TestMapperFactory`
- **WebApplicationFactory** — поднимает API in-memory для HTTP-тестов

### E2E

- **Cypress 15** — сценарии каталога, корзины, checkout
- `data-testid` на кнопках корзины для стабильных селекторов
- В CI/CD подключается позже (финальный gate перед деплоем)

## Запуск

### Требования

- .NET 8 SDK
- **Docker Desktop** (запущен) — обязателен для `InShop.IntegrationTests`

### Только unit-тесты (быстро, без Docker)

```powershell
cd source/backend/InShop.WebAPI
dotnet test InShopBLLayer.Tests/InShopBLLayer.Tests.csproj
```

### Все backend-тесты

```powershell
cd source/backend/InShop.WebAPI
dotnet test InShop.WebAPI.sln
```

### Покрытие кода

```powershell
dotnet test InShop.WebAPI.sln `
  --settings coverlet.runsettings `
  --collect:"XPlat Code Coverage" `
  --results-directory ./TestResults
```

Отчёт `coverage.cobertura.xml` появится в `TestResults/`.

### Cypress (локально)

1. Запустить API и frontend (см. инструкцию по локальному запуску).
2. Выполнить:

```powershell
cd source/frontend/in-shop/cypress-tests
npm install
npm run cypress:open   # интерактивно
# или
npm run cypress:run    # headless
```

## CI/CD в GitHub Actions

Workflow находится в `.github/workflows/ci.yml`.

Что выполняется автоматически:

- при `push` в `main`, `master`, `dev`;
- при `pull_request`.

Этапы pipeline:

- backend: `dotnet restore`, `dotnet build`, unit-тесты, integration-тесты;
- frontend: `npm ci`, `npm run build`;
- Docker: проверка `docker compose config`, сборка Docker images.

На `pull_request` Docker images только собираются для проверки Dockerfile. В registry они не публикуются.

На `push` после успешных backend/frontend проверок публикуются Docker images в GitHub Container Registry:

- `ghcr.io/<owner>/inshop-api`;
- `ghcr.io/<owner>/inshop-frontend`;
- `ghcr.io/<owner>/inshop-embedding-server`.

Теги образов:

- `latest` — только для `main`/`master`;
- `dev` — только для ветки `dev`;
- `sha-<commit>` — для каждой сборки, чтобы можно было точно восстановить образ конкретного коммита.

`PaymentsAPI` намеренно не собирается и не публикуется в CI/CD. Это dev-only mock-сервис для локальной разработки.

Production deployment пока не настроен и вынесен в отдельную будущую задачу.

## Как устроен Testcontainers (подробно)

### Идея

Обычный integration-тест требует БД. Варианты:

1. Общая dev-БД — тесты портят данные друг другу.
2. InMemory/SQLite — быстро, но не совпадает с SQL Server (`rowversion`, T-SQL).
3. **Testcontainers** — на каждый прогон (или коллекцию тестов) поднимается **настоящий** SQL Server в Docker.

### Жизненный цикл в нашем коде

Файл: `InShop.IntegrationTests/Infrastructure/SqlServerFixture.cs`

```
1. xUnit создаёт SqlServerFixture (IAsyncLifetime)
2. InitializeAsync():
   - MsSqlContainer.StartAsync()  → docker run SQL Server
   - ConnectionString = container.GetConnectionString()
   - AppDbContext.Database.EnsureCreatedAsync()
   - AdminIdentityDbContext.Database.EnsureCreatedAsync()
3. Все тесты с [Collection("SqlServer")] используют ОДИН контейнер
4. DisposeAsync() → контейнер удаляется
```

`[Collection("SqlServer")]` + `ICollectionFixture<SqlServerFixture>` — механизм xUnit, чтобы **не** поднимать SQL Server заново на каждый тест-класс (экономия 30–60 сек).

### Сброс данных между тестами

`ResetDatabaseAsync()`:

- `EnsureDeleted` + `EnsureCreated` для бизнес- и identity-контекстов
- Чистая БД перед каждым тестом, который вызывает reset

### Почему исправлен `AppDbContext.OnConfiguring`

Scaffold содержал жёсткую строку подключения. Без проверки `IsConfigured` EF игнорировал строку из Testcontainers. Теперь:

```csharp
if (!optionsBuilder.IsConfigured) { ... fallback для dev ... }
```

### WebApplicationFactory

Файл: `InShop.IntegrationTests/Api/InShopWebApplicationFactory.cs`

- Наследует `WebApplicationFactory<Program>` — поднимает ASP.NET Core приложение в памяти
- Подменяет `ConnectionStrings:DefaultConnection` на строку из контейнера
- Redis: `abortConnect=false` — приложение стартует без реального Redis
- `AiSettings:ApiKey` пустой → регистрируется `NoOpAiProvider`
- Удаляет `IHostedService` (фоновая индексация в Redis не мешает тестам)

`Program` сделан `partial` — требование `WebApplicationFactory` для top-level statements.

## Что покрыто тестами

### Unit (`InShopBLLayer.Tests`)

- `OrderStatusStateMachine` — FSM статусов заказа
- `OrderService` — корзина, пересчёт сумм, принадлежность позиции сессии
- `ReviewService.IsUniqueConstraintViolation` — распознавание UNIQUE в SQL
- `PaymentStatusService` — статус заказа / NotFound
- `CategoryService` — CRUD-обёртки над репозиторием

### Integration (`InShop.IntegrationTests`)

- `InventoryReservationService` — reserve / release / finalize, `RowVersion`
- `AdminOrderService` — смена статуса, audit log, фильтр черновиков
- `ReviewService` — дубликат отзыва, голосование
- API: `GET /api/Category`, `GET /api/Admin/orders` → 401 без JWT

## AutoMapper в тестах

- **Unit:** `IMapper` — мок Moq, реальный AutoMapper не вызывается
- **Integration:** реальные профили через `services.AddAutoMapper(...)` в `TestMapperFactory`

## Устранение неполадок

| Проблема | Решение |
|----------|---------|
| `Docker API ... pipe/dockerDesktopLinuxEngine` | Запустить Docker Desktop, дождаться статуса Running |
| Долгий первый прогон | Скачивается образ SQL Server (~1.5 GB) |
| Integration падают, unit проходят | Проверить `docker ps` |
| Cypress не находит элементы | Убедиться, что API на :7275 и frontend на :3000 |
