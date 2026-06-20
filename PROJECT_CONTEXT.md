# Контекст проекта InShop

Документ описывает состав репозитория, архитектуру, ключевые runtime-потоки, тестирование, Docker-инфраструктуру и текущее состояние CI/CD.

## 1. Что находится в репозитории

`InShop` - это учебно-практический e-commerce проект. В репозитории есть:

- основной backend на ASP.NET Core Web API;
- frontend на React;
- отдельный сервис mock-оплаты для разработки;
- Python-сервис embeddings для семантического/векторного поиска;
- интеграция с YooKassa для production-like оплаты;
- опциональная интеграция с Yandex GPT для AI-сводок отзывов;
- unit, integration и Cypress E2E-тесты;
- Docker Compose-инфраструктура;
- GitHub Actions workflow для CI и публикации Docker images.

Основные папки:

- `source/backend/InShop.WebAPI` - основное backend-решение;
- `source/backend/PaymentsAPI` - standalone mock-сервис оплаты для разработки;
- `source/backend/EmbeddingServer` - Python FastAPI-сервис embeddings;
- `source/frontend/in-shop` - клиентское React-приложение;
- `deploy` - Docker Compose-файлы и шаблон `.env`;
- `.github/workflows/ci.yml` - CI workflow.

## 2. Технологический стек

### Backend

- `.NET 8`;
- `ASP.NET Core Web API`;
- `Entity Framework Core`;
- `SQL Server`;
- `ASP.NET Core Identity` для admin-аутентификации;
- `AutoMapper`;
- `StackExchange.Redis`;
- `Swashbuckle / Swagger`;
- `RazorLight` для email-шаблонов;
- pluggable AI providers: `YandexGptProvider`, `NoOpAiProvider`;
- YooKassa как production-like payment provider;
- mock payment provider для разработки.

Ключевые backend-проекты:

- `source/backend/InShop.WebAPI/InShop.WebAPI/InShop.WebAPI.csproj`;
- `source/backend/InShop.WebAPI/InShopBLLayer/InShopBLLayer.csproj`;
- `source/backend/InShop.WebAPI/InShopDbModels/InShopDbModels.csproj`;
- `source/backend/InShop.WebAPI/Contracts/Contracts.csproj`.

### Frontend

- `React`;
- `React Router`;
- `Axios`;
- смешанная кодовая база `TypeScript` + `JavaScript`;
- `Create React App` / `react-scripts`;
- Cypress для E2E-тестов.

Ключевая папка:

- `source/frontend/in-shop`.

### Search / ML

- `FastAPI`;
- `sentence-transformers`;
- модель `sentence-transformers/LaBSE`;
- Redis vector index для поиска по товарам.

Ключевой файл:

- `source/backend/EmbeddingServer/server.py`.

### Тестирование и tooling

- `xUnit`;
- `FluentAssertions`;
- `Moq`;
- `Coverlet`;
- `Testcontainers.MsSql`;
- `WebApplicationFactory`;
- `Cypress`;
- Docker / Docker Compose;
- GitHub Actions.

## 3. Высокоуровневая архитектура

```text
React frontend
  -> ASP.NET Core Web API
      -> BLL services
          -> EF Core repositories
              -> SQL Server
      -> Redis
          -> search index
          -> AI review-summary cache
      -> PaymentProcessingService
          -> YooKassa или mock provider
      -> EmbeddingServer
          -> генерация embeddings для поиска
      -> IAiProvider
          -> Yandex GPT или NoOp stub
```

Поиск:

```text
SQL Server products
  -> VectorSearchIndexRebuildService
      -> EmbeddingServer
      -> Redis index
  -> VectorIndexingService
      -> фоновая переиндексация
  -> AdminProductService
      -> точечная индексация после CRUD
```

Отзывы:

```text
Product page
  -> ProductsController review endpoints
      -> ReviewService
      -> ReviewVoteRepository
      -> optional AI summary
          -> ReviewCacheService
          -> AiAnalysisService
          -> Yandex GPT или NoOpAiProvider
```

## 4. Основные решения и зоны ответственности

### `InShop.WebAPI`

API host проекта. Отвечает за:

- HTTP endpoints;
- middleware;
- Swagger;
- CORS;
- static files;
- DI-регистрацию;
- startup-конфигурацию;
- database bootstrap для Docker.

Важные файлы:

- `source/backend/InShop.WebAPI/InShop.WebAPI/Program.cs`;
- `source/backend/InShop.WebAPI/InShop.WebAPI/Extensions/DatabaseBootstrapExtensions.cs`;
- `source/backend/InShop.WebAPI/InShop.WebAPI/Middleware/SessionMiddleware.cs`;
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/ProductsController.cs`;
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/OrderController.cs`;
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/SearchController.cs`;
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/AdminController.cs`;
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/PaymentController.cs`;
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/WebhookController.cs`.

### `InShopBLLayer`

Слой бизнес-логики. Содержит сервисы для:

- товаров;
- категорий;
- корзины и заказов;
- user session lifecycle;
- email verification;
- платежей;
- отзывов;
- AI-сводок отзывов;
- vector search indexing;
- admin-панели;
- резервирования остатков.

Важные файлы:

- `source/backend/InShop.WebAPI/InShopBLLayer/Extensions/ServiceCollectionBLLayerExtension.cs`;
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/OrderService.cs`;
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/ReviewService.cs`;
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/PaymentProcessingService.cs`;
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Search/VectorIndexingService.cs`;
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Search/VectorSearchIndexRebuildService.cs`;
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/AdminAuthService.cs`;
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/AdminProductService.cs`;
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/AdminOrderService.cs`;
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/InventoryReservationService.cs`.

### `InShopDbModels`

Слой доступа к данным. Содержит:

- `AppDbContext`;
- entity models;
- repository interfaces;
- repository implementations;
- `AdminIdentityDbContext` для Identity-таблиц.

Важные файлы:

- `source/backend/InShop.WebAPI/InShopDbModels/Data/AppDbContext.cs`;
- `source/backend/InShop.WebAPI/InShopDbModels/Data/AdminIdentityDbContext.cs`;
- `source/backend/InShop.WebAPI/InShopDbModels/Extensions/ServiceCollectionDbModelsExtension.cs`.

### `Contracts`

Проект с DTO и контрактами между API и сервисами:

- search DTOs;
- order DTOs;
- payment DTOs;
- review DTOs;
- session DTOs;
- admin DTOs.

### `PaymentsAPI`

Standalone mock-сервис оплаты. Используется только для разработки.

Роль:

- имитирует запуск оплаты;
- ждёт перед возвратом результата;
- отправляет webhook в основной API.

Важно: `PaymentsAPI` не входит в production compose и не публикуется как Docker image в CI/CD.

### `EmbeddingServer`

Python FastAPI-сервис, который генерирует embeddings для семантического поиска.

Роль:

- принимает текст товара;
- возвращает embedding;
- используется backend-сервисами индексации.

## 5. Backend layering

### API layer

Контроллеры должны:

- принимать HTTP-запрос;
- валидировать базовые входные данные;
- вызывать BLL-сервисы;
- возвращать HTTP-ответ.

Контроллеры не должны содержать сложную бизнес-логику.

### Business layer

BLL-сервисы отвечают за:

- бизнес-правила;
- оркестрацию репозиториев;
- взаимодействие с payment provider;
- индексацию;
- работу с Redis cache;
- подготовку DTO.

### Data layer

Data layer отвечает за:

- EF Core contexts;
- entity mapping;
- SQL Server queries;
- repository methods.

## 6. Основная доменная модель

Главные сущности:

- `Product` - товар;
- `Category` - категория;
- `Order` - заказ/корзина;
- `OrderItem` - позиция заказа;
- `UserSession` - пользовательская сессия;
- `ShipCompany` - служба доставки;
- `ProductReview` - отзыв;
- `ReviewVote` - голос за отзыв;
- `ProductSpecification` / `ProductSpecValue` - характеристики товара;
- `OrderAuditLog` - аудит admin-действий по заказам;
- `AspNetUsers`, `AspNetRoles` - Identity-таблицы для admin-аутентификации.

## 7. Резервирование остатков

Система резервирования нужна, чтобы товар не был продан сверх доступного количества.

Ключевые поля `Product`:

- `ProductStockQuantity`;
- `ReservedQuantity`;
- `RowVersion`.

Основной сервис:

- `InventoryReservationService`.

Основные операции:

- reserve;
- release;
- finalize.

`RowVersion` используется для optimistic concurrency.

## 8. Admin-панель

Admin-панель включает:

- JWT-аутентификацию;
- роли через ASP.NET Core Identity;
- список заказов;
- смену статусов;
- просмотр черновиков заказов;
- CRUD товаров;
- загрузку изображений товаров;
- аудит изменений заказов.

Большинство admin endpoints защищены политикой `AdminOnly`.

Frontend admin-код находится в:

- `source/frontend/in-shop/src/admin`.

## 9. Frontend-структура

Ключевые части frontend:

- `src/api` - API clients;
- `src/config/api.js` - централизованная настройка API URL;
- `src/context/SessionContext.tsx` - состояние пользовательской сессии;
- `src/components/CartContext.js` - состояние корзины;
- `src/components/SearchComponent` - поиск;
- `src/components/FiltersPanel` - фильтры;
- `src/components/ReviewList` и `ReviewItem` - отзывы;
- `src/admin` - admin UI.

## 10. Runtime-потоки

### Session lifecycle

1. Frontend открывает приложение.
2. `SessionContext` запрашивает или создаёт user session.
3. Backend выдаёт session cookie.
4. Корзина и заказы связываются с текущей сессией.

### Cart and checkout

1. Пользователь добавляет товар в корзину.
2. Backend создаёт или обновляет order/order item.
3. Frontend показывает корзину.
4. Checkout собирает ФИО, email, телефон, доставку и оплату.
5. Backend отправляет код подтверждения email.
6. После подтверждения заказ переходит к оплате или финализации.

### Payment flow

Production-like поток:

1. Backend создаёт payment request через YooKassa.
2. Пользователь проходит оплату.
3. YooKassa отправляет webhook.
4. Backend проверяет статус.
5. Заказ получает актуальный payment/order status.

Dev/mock поток:

1. Backend вызывает `PaymentsAPI`.
2. `PaymentsAPI` имитирует задержку оплаты.
3. `PaymentsAPI` отправляет webhook в основной backend.

### Search flow

1. Товары индексируются в Redis.
2. Для текста товара embedding генерирует `EmbeddingServer`.
3. Search endpoint принимает query и filters.
4. Backend комбинирует lexical/vector/filter-логику.
5. Frontend показывает результаты и рекомендации.

### Reviews flow

1. Пользователь оставляет отзыв.
2. Backend проверяет права/уникальность.
3. Отзыв сохраняется.
4. Рейтинг товара пересчитывается.
5. Пользователи могут голосовать за полезность отзыва.

### AI review summary flow

1. Product page запрашивает AI summary.
2. Backend проверяет Redis cache.
3. Если cache пустой, `AiAnalysisService` вызывает `IAiProvider`.
4. В production-like режиме используется Yandex GPT.
5. В тестах и без API key используется `NoOpAiProvider`.

## 11. Конфигурация и entry points

### Backend

Основной entry point:

- `source/backend/InShop.WebAPI/InShop.WebAPI/Program.cs`.

Важные настройки:

- `ConnectionStrings:DefaultConnection`;
- `Redis:ConnectionString`;
- `Embedding:BaseUrl`;
- `AiSettings:*`;
- `Payment:Provider`;
- `YooKassa:*`;
- `Database:EnsureCreated`;
- SMTP/email settings.

### Frontend

Важная настройка:

- `REACT_APP_API_BASE_URL`.

Если переменная не задана, frontend использует `/api`.

### Docker

Основные файлы:

- `deploy/docker-compose.yml`;
- `deploy/docker-compose.dev.yml`;
- `deploy/.env.example`;
- `source/backend/InShop.WebAPI/InShop.WebAPI/Dockerfile`;
- `source/backend/EmbeddingServer/Dockerfile`;
- `source/frontend/in-shop/Dockerfile`;
- `source/frontend/in-shop/nginx.conf`.

## 12. Локальная инфраструктура

Для полного локального запуска нужны:

- SQL Server;
- Redis Stack Server;
- EmbeddingServer;
- backend API;
- frontend;
- опционально `PaymentsAPI` для mock-оплаты.

Для Docker-запуска используется папка `deploy`.

## 13. Тестирование

Backend-тесты:

- `InShopBLLayer.Tests` - unit-тесты бизнес-логики;
- `InShop.IntegrationTests` - integration/API-тесты с реальным SQL Server через Testcontainers.

Frontend E2E:

- `source/frontend/in-shop/cypress-tests`.

Документ с подробностями:

- `source/backend/InShop.WebAPI/TESTING.md`.

В integration-тестах:

- используется `WebApplicationFactory`;
- SQL Server поднимается через Testcontainers;
- фоновые hosted services отключаются;
- SMTP/email заменяется на `NoOpEmailSender`;
- Redis и AI не должны ломать запуск тестов.

## 14. Docker и CI/CD

GitHub Actions workflow:

- `.github/workflows/ci.yml`.

Workflow запускается:

- при `push` в `main`, `master`, `dev`;
- при `pull_request`.

Этапы:

- backend restore/build/tests;
- frontend install/build;
- Docker Compose config validation;
- Docker image build;
- Docker image publish в GHCR на push.

Публикуемые images:

- `ghcr.io/<owner>/inshop-api`;
- `ghcr.io/<owner>/inshop-frontend`;
- `ghcr.io/<owner>/inshop-embedding-server`.

Теги:

- `latest` - только из `main`/`master`;
- `dev` - только из `dev`;
- `sha-<commit>` - для каждой push-сборки.

Production deployment пока не настроен. Он вынесен в отдельную будущую задачу.

## 15. YooKassa

Провайдер оплаты выбирается через конфигурацию.

Production-like режим:

```env
Payment__Provider=YooKassa
```

Dev/mock режим:

```env
Payment__Provider=Mock
PaymentsAPI__BaseUrl=http://payments-api:8080
```

Для YooKassa нужны:

- shop id;
- secret key;
- return URL;
- webhook endpoint.

В production-like compose `YOOKASSA_*` значения должны быть указаны явно.

## 16. Сильные стороны архитектуры

- Чёткое разделение API/BLL/Data.
- Реальные integration-тесты с SQL Server.
- Admin Identity вынесен отдельно от scaffolded business-модели.
- PaymentsAPI отделён от production-like стека.
- Docker Compose позволяет поднять полный стек локально.
- CI проверяет backend, frontend и Docker-сборку.
- Docker images публикуются в GHCR без production-деплоя.

## 17. Риски и технические наблюдения

- Database-first модель требует осторожного scaffold: нельзя затирать Identity и partial-расширения.
- `EnsureCreated` удобен для Docker/dev, но не заменяет production migration strategy.
- Redis и EmbeddingServer важны для поиска, поэтому degraded mode должен быть понятным.
- SMTP/email-настройки обязательны в реальной среде, но в тестах должны подменяться.
- `latest` Docker tag должен использоваться только для стабильных веток.
- Production deployment требует отдельного проектирования: сервер, secrets, GHCR auth, backup, rollback.

## 18. Ментальная модель для работы с репозиторием

При изменениях в проекте полезно держать такую цепочку:

```text
Controller
  -> BLL service
      -> repository / external provider
          -> SQL Server / Redis / YooKassa / EmbeddingServer
```

Для frontend:

```text
page/component
  -> context или hook
      -> api client
          -> backend endpoint
```

Для CI/CD:

```text
push / pull_request
  -> backend checks
  -> frontend checks
  -> Docker build
  -> GHCR publish только на push
```

## 19. Короткое резюме

`InShop` - e-commerce система с ASP.NET Core backend, React frontend, SQL Server, Redis, semantic search, YooKassa payment integration, admin-панелью, автотестами, Docker Compose и GitHub Actions CI/CD.

Текущий CI/CD уже проверяет код, собирает Docker images и публикует production-like images в GHCR. Production deployment пока намеренно не реализован.
