# InShop Project Context

## 1. What This Repository Contains

`InShop` is an e-commerce system with:

- a primary backend on ASP.NET Core Web API
- a frontend on React
- a separate payment simulation service
- a Python embedding service for semantic/vector search
- optional Yandex GPT integration for AI-generated review summaries

The repository is split into:

- `source/backend/InShop.WebAPI` - main backend solution
- `source/backend/PaymentsAPI` - standalone payment simulator service
- `source/backend/EmbeddingServer` - Python service that generates embeddings
- `source/frontend/in-shop` - client application

There is also a local launch instruction file at the repo root: `Инструкция по локальному запуску.html`.

## 2. Technology Stack

### Backend

- `.NET 8`
- `ASP.NET Core Web API`
- `Entity Framework Core` with `SQL Server`
- `AutoMapper`
- `StackExchange.Redis` (search index + review AI-summary cache)
- `Swashbuckle / Swagger`
- `RazorLight` for email templates
- pluggable AI providers (`YandexGptProvider`, `NoOpAiProvider`)

Key backend project files:

- `source/backend/InShop.WebAPI/InShop.WebAPI/InShop.WebAPI.csproj`
- `source/backend/InShop.WebAPI/InShopBLLayer/InShopBLLayer.csproj`
- `source/backend/InShop.WebAPI/InShopDbModels/InShopDbModels.csproj`

### Frontend

- `React 19`
- `React Router 7`
- `Axios` (centralized `apiClient` with credentials and 401 retry)
- `TypeScript` + `JavaScript` mixed codebase
- `Create React App` / `react-scripts`
- `Swiper` for the search recommendations carousel

Key frontend file:

- `source/frontend/in-shop/package.json`

### Search / ML

- `FastAPI`
- `sentence-transformers`
- model: `sentence-transformers/LaBSE`
- Redis vector index built from product catalog data

Key ML service file:

- `source/backend/EmbeddingServer/server.py`

### Testing / Tooling

- `Cypress` for E2E tests
- Docker for the main backend + SQL Server
- Visual Studio / launch profiles for local development

## 3. High-Level Architecture

```text
React frontend
  -> ASP.NET Core Web API
      -> BLL services
          -> EF Core repositories
              -> SQL Server
      -> Redis (vector + lexical search index, AI review-summary cache)
      -> PaymentProcessingService
          -> PaymentsAPI
              -> webhook callback
                  -> Main Web API updates order status
      -> IAiProvider (Yandex GPT or NoOp stub)
          -> AiAnalysisService generates review summaries

Vector indexing/search:
SQL Server products
  -> VectorSearchIndexRebuildService (full rebuild + per-product upsert)
      -> Python EmbeddingServer
      -> Redis index (idx:products, product:{id})
  -> VectorIndexingService (hosted, hourly full rebuild via scoped scope)
  -> AdminProductService (IndexProductAsync / RemoveProductAsync after CRUD)

Reviews:
Product page
  -> ProductsController review endpoints
      -> ReviewService (CRUD, votes, verified-purchase flag)
      -> optional GET .../reviews/ai-summary
          -> ReviewCacheService (Redis)
          -> AiAnalysisService -> Yandex GPT
```

## 4. Main Solutions and Responsibilities

### 4.1 Main backend solution

Solution:

- `source/backend/InShop.WebAPI/InShop.WebAPI.sln`

Projects inside it:

#### `InShop.WebAPI`

Role: API host, controllers, middleware, HTTP configuration, Swagger, CORS, static files.

Important files:

- `source/backend/InShop.WebAPI/InShop.WebAPI/Program.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Middleware/SessionMiddleware.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/ProductsController.cs` (products + reviews + AI summary)
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/CategoryController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/OrderController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/UserSessionController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/SearchController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/PaymentController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/WebhookController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/VerificationController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/ShipCompanyController.cs`

#### `InShopBLLayer`

Role: business logic and orchestration.

Contains service abstractions and implementations for:

- products
- categories
- shipping companies
- orders / cart
- session lifecycle
- email verification
- payment status
- reviews (CRUD, voting, verified-purchase check, rating sync on product)
- review AI-summary cache (Redis)
- AI analysis of review texts (`AiAnalysisService`, `IAiProvider`)
- vector indexing (`VectorSearchIndexRebuildService`, `VectorIndexingService`)
- admin panel (`AdminAuthService`, `AdminProductService`, `AdminOrderService`)
- inventory reservation (`InventoryReservationService`)

Important files:

- `source/backend/InShop.WebAPI/InShopBLLayer/Extensions/ServiceCollectionBLLayerExtension.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/ReviewService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/ReviewCacheService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/AiAnalysisService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Ai/Providers/YandexGptProvider.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Ai/Providers/NoOpAiProvider.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Search/VectorIndexingService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Search/VectorSearchIndexRebuildService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Abstractions/IVectorSearchIndexRebuildService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/AdminAuthService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/AdminProductService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/AdminOrderService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/ProductImageStorage.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/InventoryReservationService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Abstractions/IInventoryReservationService.cs`

#### `InShopDbModels`

Role: data access and persistence.

Contains:

- `AppDbContext`
- entity models
- repository interfaces
- repository implementations

Important files:

- `source/backend/InShop.WebAPI/InShopDbModels/Data/AppDbContext.cs`
- `source/backend/InShop.WebAPI/InShopDbModels/Extensions/ServiceCollectionDbModelsExtension.cs`
- `source/backend/InShop.WebAPI/InShopDbModels/Repositories/ProductReviewRepository.cs`
- `source/backend/InShop.WebAPI/InShopDbModels/Repositories/ReviewVoteRepository.cs`

#### `Contracts`

Role: DTOs shared across API and service boundaries.

Examples:

- search DTOs
- order DTOs
- payment DTOs
- review DTOs (`CreateReviewDto`, `ReviewResponseDto`, `ReviewVoteDto`, `ReviewSummaryDto`)
- session DTOs

### 4.2 Standalone payment simulator

Solution:

- `source/backend/PaymentsAPI/PaymentsAPI.sln`

Role:

- simulates payment initiation
- waits before returning a result
- sends a webhook to the main API to mark the order as paid

Important files:

- `source/backend/PaymentsAPI/PaymentsAPI/Program.cs`
- `source/backend/PaymentsAPI/PaymentsAPI/Controllers/PaymentController.cs`

### 4.3 Python embedding service

Role:

- receives text
- generates a 768-d embedding
- returns it to the .NET backend

Important file:

- `source/backend/EmbeddingServer/server.py`

## 5. Backend Layering

The main backend follows a layered structure:

### API layer

Responsibilities:

- receive HTTP requests
- validate request shape
- resolve current session
- call business services
- return DTOs / status codes

Examples:

- `OrderController` handles cart and checkout endpoints
- `UserSessionController` creates and validates session cookies
- `SearchController` performs hybrid search and returns a separate `recommended` list
- `ProductsController` handles catalog endpoints and all review/AI-summary endpoints under `/api/Products`
- `PaymentController` starts external payment processing
- `WebhookController` receives payment result callbacks

### Business layer

Responsibilities:

- domain/business workflows
- orchestration across repositories
- mapping entities to DTOs
- cache/search background processes
- email and verification logic
- review workflows and AI summary generation

### Data layer

Responsibilities:

- EF Core context
- repository implementations
- entity persistence
- SQL Server interaction

## 6. Domain Model

Based on `AppDbContext`, the main domain entities are:

- `Product` (includes denormalized `ReviewsCount` and `AverageRating`; inventory: `ProductStockQuantity`, `ReservedQuantity`, `RowVersion`)
- `Category`
- `Order`
- `OrderItem`
- `UserSession`
- `ShipCompany`
- `ProductReview`
- `ReviewVote`
- `ProductSpecification`
- `ProductSpecGroup`
- `ProductSpecValue`
- `ProductSpecLink`
- `Admin`

This indicates the product catalog supports:

- categories
- stock and availability
- shipping providers
- session-based carts
- product reviews with voting (one review per session per product)
- verified-purchase badge derived from order history
- structured product specifications used in filtering/search
- warehouse reservation with optimistic concurrency (`ReservedQuantity` + `RowVersion`)

## 6.1 Inventory & Reservation System

### Purpose

Prepares safe stock deduction when order status changes (admin or payment flow). Cart/session/payment customer flows are **not** modified yet; reservation is invoked explicitly via `IInventoryReservationService` or future `AdminOrderService` integration.

### Entity fields (`Product`)

| Field | Role |
|-------|------|
| `ProductStockQuantity` | Free (unreserved) stock available for new `Reserve` calls |
| `ReservedQuantity` | Units allocated to orders, not yet finalized |
| `RowVersion` | SQL Server `rowversion` token for optimistic concurrency |

**Invariants:**

- Physical on-hand = `ProductStockQuantity + ReservedQuantity`
- **Reserve:** `ProductStockQuantity -= n`, `ReservedQuantity += n` (physical total unchanged)
- **Release:** reverse of Reserve (e.g. order cancelled)
- **Finalize:** `ReservedQuantity -= n` only (sale confirmed; physical stock decreases by `n`)

### Patterns

- **Optimistic concurrency:** `[Timestamp]` / `IsRowVersion()` — parallel `UPDATE` on the same product row causes `DbUpdateConcurrencyException` instead of silent lost updates (race condition).
- **Retry loop (max 3):** on conflict, rollback transaction, `ChangeTracker.Clear()`, reload product, re-apply delta.
- **Transactions:** each public method runs inside `IDbContextTransaction` (`BeginTransactionAsync` → `SaveChangesAsync` → `Commit`).
- **Why not pessimistic locks:** shorter lock duration, better read throughput on catalog; conflicts retried in application layer.

### Key files

- Model: `source/backend/InShop.WebAPI/InShopDbModels/Models/Product.cs`
- EF config: `source/backend/InShop.WebAPI/InShopDbModels/Data/AppDbContext.cs` (`ReservedQuantity` default 0, `RowVersion` `IsRowVersion()`)
- Interface: `source/backend/InShop.WebAPI/InShopBLLayer/Abstractions/IInventoryReservationService.cs`
- Implementation: `source/backend/InShop.WebAPI/InShopBLLayer/Services/Admin/InventoryReservationService.cs`
- Integration stub: `AdminOrderService` — TODO: call `InventoryReservationService` on status transitions
- DI: `source/backend/InShop.WebAPI/InShopBLLayer/Extensions/ServiceCollectionBLLayerExtension.cs`
- **Database migration (manual):** `source/backend/InShop.WebAPI/scripts/AddProductReservationColumns.sql` — run once per database in SSMS against `InShopDB` before starting the API (see script header for steps)

### Not integrated yet

- `OrderService` / cart do not call `ReserveAsync`
- Search Redis index still uses `ProductStockQuantity` only
- `InventoryReservationService` not yet called from `AdminOrderService` (TODO in code)

## 6.2 Admin Panel

Админка изолирована от покупательской сессии: маршруты `/admin/*`, JWT в `sessionStorage`, не затрагивает `SessionToken` / корзину.

### Authentication

- **ASP.NET Core Identity** on `IdentityUser` + **JWT Bearer** (8h, `appsettings.json` → `Jwt`).
- Corporate **email = UserName**; role `Admin`; policy `AdminOnly`.
- Customer `SessionToken` / `SessionMiddleware` unchanged.
- Temporary: `POST /api/Admin/auth/register` — only when `AspNetUsers` is empty; then **409**. Remove endpoint after first admin in production.
- `POST /api/Admin/auth/login`, `GET /api/Admin/auth/me`.

### Order status FSM (admin)

Canonical: `Draft` → `Unpaid` → `Processing` → `Paid` → `Shipped` → `Delivered`; `Cancelled` from any except `Delivered`.

Legacy normalization (read/validate only, customer code unchanged): `Unpayed`→`Unpaid`, `Payed`→`Paid`, etc. Admin writes canonical statuses.

Terminal statuses (`Delivered`, `Cancelled`): смена статуса запрещена в UI и в `AdminOrderService.ChangeOrderStatusAsync` (`OrderStatusStateMachine.IsTerminalStatus`).

Audit: `OrderAuditLog` (same transaction as status change).

### API (all `[Authorize(AdminOnly)]` except auth register/login)

| Method | Path |
|--------|------|
| GET | `/api/Admin/products`, `/api/Admin/products/{id}` |
| POST/PUT/DELETE | `/api/Admin/products`, `/{id}` |
| GET | `/api/Admin/orders`, `/api/Admin/orders/draft`, `/api/Admin/orders/{id}` |
| PUT | `/api/Admin/orders/{id}/status` |
| GET | `/api/Admin/orders/{id}/allowed-statuses` |

**DTO:** `AdminProductUpdateDto.RemoveImage` — открепить изображение (очистить `ImageUrl`, удалить файл из `uploads/products/`).

### Product images (admin upload)

- Frontend отправляет `imageBase64` (data URL или Base64, ≤ **5 MB**, JPEG/PNG/WebP).
- `ProductImageStorage` → `InShop.WebAPI/wwwroot/uploads/products/{guid}.ext`
- В БД: `/uploads/products/{fileName}`; статика через `UseStaticFiles()`.
- При замене или `RemoveImage` локальный старый файл удаляется; внешние URL не трогаются.

### Redis search index (admin CRUD)

`IVectorSearchIndexRebuildService` / `VectorSearchIndexRebuildService`:

| Method | When | Action |
|--------|------|--------|
| `IndexProductAsync(productId)` | create/update в админке | `HSET product:{id}`; `EnsureIndexExistsAsync` при первом save |
| `RemoveProductAsync(productId)` | delete в админке | `DEL product:{id}` |
| `RebuildFullIndexAsync()` | `VectorIndexingService` (~1 ч) | `FT.DROPINDEX` → все hash → `FT.CREATE` |

- Общая логика — `UpsertProductHashAsync`; ошибка индексации не откатывает SQL (try/catch в `AdminProductService`).
- `VectorIndexingService` (singleton) использует `IServiceScopeFactory` для scoped DI.
- **Limitation:** новое spec-поле в schema может потребовать full rebuild / `FT.ALTER`.

### Admin UI (frontend: `src/admin/`)

| Feature | Files |
|---------|-------|
| Форма товара | `AdminProductForm.tsx` — чекбокс 20px слева, цена `step=1` |
| Превью / lightbox | `AdminImagePreview.tsx`, `adminUtils.resolveProductImageUrl` |
| Открепить фото | кнопка → `removeImage: true` |
| Спиннер / успех | `AdminLoadingOverlay.tsx`, `AdminNoticeModal.tsx` |
| Пагинация | `AdminPagination.tsx` — над и под таблицами |
| Заказы | `AdminOrdersList.tsx`, `OrderDetailsModal.tsx`, `OrderStatusModal.tsx` |
| Самовывоз | в «Подробнее» поле **ТК** скрыто (`isPickupShipMethod`) |

Утилиты: `adminUtils.ts` — `isTerminalOrderStatus`, `isPickupShipMethod`, `resolveProductImageUrl`.

### Key backend files

- `InShopDbModels/Data/AppDbContext.cs`, `AdminIdentityDbContext.cs`
- `InShopDbModels/Models/OrderAuditLog.cs`
- `InShopBLLayer/Services/Admin/*`, `Services/Search/VectorSearchIndexRebuildService.cs`, `VectorIndexingService.cs`
- `InShopBLLayer/Abstractions/IVectorSearchIndexRebuildService.cs`, `IAdmin*Service.cs`
- `Contracts/Admin/Dto/*` (в т.ч. `AdminOrderDetailDto`)
- `InShop.WebAPI/Controllers/Admin/*`, `Extensions/AdminIdentityExtensions.cs`

### Frontend routes

- JWT: `src/admin/api/adminClient.ts`
- Routes: `/admin/login`, dashboard, products, orders, drafts — `AdminRoutes.tsx`, `AdminLayout.tsx`

### Database setup (database-first / scaffold)

**Два контекста, одна БД:** `AppDbContext` + `AdminIdentityDbContext`.

Порядок SQL в SSMS:

1. `scripts/CreateAspNetIdentityTables.sql`
2. `scripts/AddAdminIdentityAndAudit.sql`
3. `scripts/AddProductReservationColumns.sql` (если нужно)

Scaffold: `source/backend/InShop.WebAPI/scripts/SCAFFOLDING.md`  
Partial: `AppDbContext.NonScaffolded.partial.cs`

## 7. Frontend Structure

Frontend root:

- `source/frontend/in-shop`

Main entry points:

- `source/frontend/in-shop/src/index.js`
- `source/frontend/in-shop/src/App.tsx`
- `source/frontend/in-shop/src/components/AppRoutes.tsx`

### API client

Central HTTP layer:

- `source/frontend/in-shop/src/api/client.ts`

Behavior:

- base URL: `https://localhost:7275/api`
- `withCredentials: true` for `HttpOnly` session cookie
- on `401`, recreates session via `POST /UserSession` and retries once
- domain-specific modules: `src/api/reviews.ts`

### Frontend responsibilities

#### Application shell

- `App.tsx` wires shop routes (`/*`) and admin routes (`/admin/*`) separately; shop branch uses session provider, cart, header/footer.

#### Routing

Routes currently include:

- catalog (`/`, `/catalog`)
- category page (`/category/:categoryName`)
- product page (`/product/:productId`) — reviews, rating, AI summary
- search page (`/search`) — results + recommendations carousel
- checkout
- payment
- payment confirmation
- order success
- email verification
- 404 fallback

#### Session state

Main files:

- `source/frontend/in-shop/src/context/SessionContext.tsx`
- `source/frontend/in-shop/src/hooks/useSession.ts`
- `source/frontend/in-shop/src/services/SessionService.ts`
- `source/frontend/in-shop/src/components/SessionHandler.tsx`

Behavior:

- frontend stores `orderId` and `sessionId` in `localStorage`
- backend stores `SessionToken` in `HttpOnly` cookie
- session validation/recreation is automatic
- `apiClient` interceptor recreates the session on `401`

#### Cart state

Main file:

- `source/frontend/in-shop/src/components/CartContext.js`

Behavior:

- cart operations go through backend order endpoints
- session token is not sent manually; backend reads it from cookie
- cart is fetched lazily and revalidated when needed

#### Product reviews UI

Main files:

- `source/frontend/in-shop/src/pages/ProductPage.tsx`
- `source/frontend/in-shop/src/components/ReviewList/ReviewList.tsx` — paginated list with "load more"
- `source/frontend/in-shop/src/components/ReviewItem/ReviewItem.tsx` — vote, edit, verified badge
- `source/frontend/in-shop/src/components/ReviewForm/ReviewForm.tsx`
- `source/frontend/in-shop/src/components/StarRating/StarRating.tsx`
- `source/frontend/in-shop/src/components/AiSummaryBlock/AiSummaryBlock.tsx`
- `source/frontend/in-shop/src/types/review.ts`
- `source/frontend/in-shop/src/types/reviewSummary.ts`

Behavior:

- reviews loaded in pages of 5 with client-side "load more"
- one review per session per product (409 on duplicate)
- upvote/downvote per session per review
- `isVerifiedPurchase` shown as "Проверенный покупатель" when the session has bought the product
- AI summary fetched from `GET /api/Products/{id}/reviews/ai-summary`

#### Search UI

Main files:

- `source/frontend/in-shop/src/hooks/useProductSearch.ts`
- `source/frontend/in-shop/src/pages/SearchResultPage/SearchResultsPage.tsx`
- `source/frontend/in-shop/src/components/SearchComponent/SearchComponent.tsx`
- `source/frontend/in-shop/src/components/FiltersPanel/FiltersPanel.tsx`
- `source/frontend/in-shop/src/components/SortMenu/SortMenu.tsx`

Behavior:

- frontend sends POST requests to search API
- supports pagination ("load more" on main results), sorting, stock filter, price filters, category filter, and spec filters
- keeps separate `results` and `recommended` lists
- recommendations rendered in a `Swiper` carousel below search results
- **empty state:** message shown when main `results` is empty (even if `recommended` is non-empty); idle hint on `/search` without criteria; API JSON supports both camelCase and PascalCase property names (`useProductSearch.ts`)

## 8. Core Runtime Flows

### 8.1 Session lifecycle

1. Frontend initializes through `SessionHandler`.
2. `useSession` checks whether a session is active.
3. If no valid session exists, frontend calls `POST /api/UserSession`.
4. Backend creates a `UserSession`, creates a draft order, and writes a secure `SessionToken` cookie.
5. Future requests rely on the cookie and middleware/controller validation.

Key files:

- `source/frontend/in-shop/src/hooks/useSession.ts`
- `source/frontend/in-shop/src/api/client.ts`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/UserSessionController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Middleware/SessionMiddleware.cs`

### 8.2 Cart and checkout

1. Frontend adds products through order endpoints.
2. Backend resolves the current session from the cookie.
3. Cart operations are tied to the session, not to an authenticated user account.
4. Checkout finalizes order details for the current draft cart/order.

Key backend API:

- `POST /api/Order`
- `GET /api/Order/cart`
- `PUT /api/Order/updateQuantity`
- `DELETE /api/Order/{orderItemId}`
- `DELETE /api/Order/clear`
- `POST /api/Order/checkout`

### 8.3 Payment flow

**Mock (`Payment:Provider` = `Mock`):**

1. Frontend payment page posts card data and `orderId` to main API.
2. Main API validates that the order exists and is unpaid.
3. `PaymentProcessingService` calls external `PaymentsAPI`.
4. `PaymentsAPI` simulates payment delay and sends webhook to main API.
5. Main API webhook updates order status.
6. Frontend can query payment status.

**YooKassa (`Payment:Provider` = `YooKassa`):**

1. `OrderSuccessPage` → `POST /api/Payment/initiate` with `{ orderId }` (no card form).
2. Backend creates payment, saves `YooKassaPaymentId` in `Orders`, returns `redirectUrl`.
3. User pays on YooKassa; returns to `/payment-confirmation?orderId=...`.
4. `POST /api/Payment/confirm-yookassa` syncs status from YooKassa API → `OrderStatus` and `PayStatus` = `Payed` (fallback if webhook is delayed).
5. `POST /api/webhook/yookassa` sets `OrderStatus` and `PayStatus` to `Payed` when `payment.succeeded` arrives (primary path).
6. `PaymentConfirmationPage` short-polls `GET /api/Payment/status/{orderId}` until `Payed`.

See also **§17** for configuration and testing.

Key files:

- `source/frontend/in-shop/src/pages/OrderSuccessPage/OrderSuccessPage.js`
- `source/frontend/in-shop/src/pages/PaymentConfirmationPage/PaymentConfirmationPage.js`
- `source/frontend/in-shop/src/pages/PaymentPage.js` (mock only)
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/PaymentController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Services/PaymentProcessingService.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Services/Payment/YooKassaPaymentService.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/WebhookController.cs`
- `source/backend/PaymentsAPI/PaymentsAPI/Controllers/PaymentController.cs` (mock only)

### 8.4 Search flow

1. **Full rebuild (hourly):** `VectorIndexingService` → scoped `RebuildFullIndexAsync` — all products from SQL → embeddings → Redis hashes → `FT.CREATE idx:products`.
2. **Incremental (admin):** `AdminProductService` after CRUD → `IndexProductAsync` / `RemoveProductAsync` (single `product:{id}` hash, no index drop).
3. Product text is assembled from category, title, description, and specs.
4. Text is sent to the Python embedding service.
5. Embeddings and searchable metadata are stored in Redis hashes (`product:{id}`).
6. `SearchController` runs lexical and vector searches in Redis and merges scores into hybrid search results.
7. Response includes paged `results` and up to 10 `recommended` products (vector KNN, deduplicated from main results).

Key files:

- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Search/VectorSearchIndexRebuildService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Search/VectorIndexingService.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/SearchController.cs`
- `source/backend/EmbeddingServer/server.py`
- `source/frontend/in-shop/src/pages/SearchResultPage/SearchResultsPage.tsx`

### 8.5 Reviews flow

1. User opens product page; `ReviewList` loads `GET /api/Products/{id}/reviews?page=&pageSize=`.
2. Reviews are sorted by vote score descending.
3. Authenticated session (cookie) required for create/update/delete/vote.
4. `POST /api/Products/{id}/reviews` creates a review; unique constraint enforces one review per session per product.
5. `POST /api/Products/reviews/{reviewId}/vote` records upvote (+1) or downvote (-1).
6. `IsVerifiedPurchase` is computed by checking whether the session has the product in a completed order (`OrderItemRepository.CheckVerifiedPurchaseAsync`).
7. After mutations, `ReviewService` recalculates `AverageRating` and `ReviewsCount` on the product.

Key backend API:

- `GET /api/Products/{id}/reviews`
- `POST /api/Products/{id}/reviews`
- `PUT /api/Products/reviews/{reviewId}`
- `DELETE /api/Products/reviews/{reviewId}`
- `POST /api/Products/reviews/{reviewId}/vote`

Key files:

- `source/backend/InShop.WebAPI/InShopBLLayer/Services/ReviewService.cs`
- `source/frontend/in-shop/src/api/reviews.ts`

### 8.6 AI review summary flow

1. Frontend `AiSummaryBlock` calls `GET /api/Products/{id}/reviews/ai-summary`.
2. Controller checks Redis cache (`ReviewCacheService`); cache hit if summary exists and `reviewCount` matches current count.
3. On miss, acquires a per-product Redis lock; concurrent requests get `503`.
4. Loads up to 50 recent review texts via `ReviewService.GetRecentReviewTextsAsync`.
5. `AiAnalysisService` sends a structured JSON prompt to `IAiProvider` (Yandex GPT when configured, otherwise `NoOpAiProvider` returns null).
6. Parsed `ReviewSummaryDto` (pros, cons, summary, rating_trend) is cached for 24 hours.
7. Cache is invalidated when review count changes.

Configuration (`appsettings.json` → `AiSettings`):

- `ProviderType` — e.g. `"Yandex"`
- `ApiKey`, `FolderId`, `ModelName`, `TimeoutSeconds`
- if `ApiKey` is empty, `NoOpAiProvider` is registered and AI summary generation will not work

Key files:

- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/ProductsController.cs` (`GetReviewAiSummary`)
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/AiAnalysisService.cs`
- `source/backend/InShop.WebAPI/InShopBLLayer/Services/ReviewCacheService.cs`
- `source/frontend/in-shop/src/components/AiSummaryBlock/AiSummaryBlock.tsx`

## 9. Search Architecture Details

The project contains a relatively advanced search subsystem:

- vector embeddings generated externally by Python
- Redis used as a hybrid search engine
- product specs included in index schema
- hybrid score combines lexical and vector scores
- filters support category, price, stock, and dynamic specification fields
- separate recommendation list (KNN, limit 10) exposed as `recommended` in `SearchResponseDto`

Important implementation notes:

- vector dimension is `768`
- Redis key prefix is `product:`
- main index name is `idx:products`
- **full** index rebuild: hosted `VectorIndexingService` (~1 hour interval)
- **incremental** upsert/delete: `AdminProductService` → `IndexProductAsync` / `RemoveProductAsync`
- shared hash builder: `VectorSearchIndexRebuildService.UpsertProductHashAsync`
- search recommendations are separate from the main paged result list and shown in a Swiper carousel on the frontend

## 10. Configuration and Entry Points

### Main backend

- `source/backend/InShop.WebAPI/InShop.WebAPI/Program.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/appsettings.json`
- `source/backend/InShop.WebAPI/InShop.WebAPI/appsettings.Development.json`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Properties/launchSettings.json`

Notable `appsettings.json` sections:

- `ConnectionStrings:DefaultConnection` — SQL Server
- `ConnectionStrings:Redis` — defaults to `localhost:6379` in `Program.cs` if omitted
- `PaymentsApi:BaseUrl` — payment simulator URL
- `Email` — SMTP settings for verification emails
- `AiSettings` — Yandex GPT provider configuration

### Payment simulator

- `source/backend/PaymentsAPI/PaymentsAPI/Program.cs`
- `source/backend/PaymentsAPI/PaymentsAPI/appsettings.Development.json`
- `source/backend/PaymentsAPI/PaymentsAPI/Properties/launchSettings.json`

### Frontend

- `source/frontend/in-shop/package.json`
- `source/frontend/in-shop/public/index.html`
- `source/frontend/in-shop/src/index.js`
- `source/frontend/in-shop/src/api/client.ts` — API base URL and interceptors

### Docker

- `source/backend/InShop.WebAPI/docker-compose.yml`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Dockerfile`

## 11. Local Infrastructure Dependencies

The code expects these local services/components:

- SQL Server
- Redis on `localhost:6379`
- Embedding server on `http://localhost:8000`
- frontend dev server on `http://localhost:3000`
- main backend API on `https://localhost:7275`
- payment service on `http://localhost:5001` (per `PaymentsApi:BaseUrl`)
- optional: Yandex Cloud API access for AI review summaries

## 12. Testing Situation

Detected automated tests:

- Cypress E2E tests in `source/frontend/in-shop/cypress-tests/cypress/e2e`

Examples:

- product browsing
- cart management
- checkout validation

I did not detect a dedicated backend unit/integration test project in the repository snapshot analyzed.

## 13. Architectural Strengths

- clear backend layering: API -> BLL -> repositories
- explicit DTO/Contracts project
- session/cart flow separated from full user-auth complexity
- advanced hybrid search design using structured specs + embeddings
- payment flow isolated behind a separate service boundary
- frontend session bootstrapping is centralized in `apiClient`
- reviews tied to sessions with voting and verified-purchase semantics
- AI summaries cached in Redis with stampede protection (lock + review-count invalidation)
- pluggable AI provider abstraction

## 14. Architectural Risks / Technical Observations

These are important context notes for future development:

- The repository contains multiple local/runtime configuration points that appear environment-specific (machine names in connection strings, etc.).
- `AppDbContext` may still contain a hardcoded SQL Server connection string in `OnConfiguring`, even though DI also supplies a connection string — verify before deploying.
- `Program.cs` registers CORS policy `"AllowSpecificOrigin"` but also calls `app.UseCors("AllowFrontend")`, which is not registered — the second call is likely a no-op or misconfiguration.
- `appsettings.json` in the repo may contain real SMTP credentials, payment URLs, and Yandex API keys — treat as secrets; use user secrets / environment variables for local dev and never commit production keys.
- The frontend is a mixed JS/TS codebase, which increases maintenance cost and type inconsistency risk.
- Some pages still use raw `axios` with a hardcoded base URL instead of `apiClient` (e.g. parts of `ProductPage.tsx`).
- Payment simulation logs raw card information in the standalone `PaymentsAPI`, which is acceptable only for local/demo use.
- The payment flow is asynchronous and webhook-driven, so order state consistency depends on both services being available.
- The vector index is periodically rebuilt by a background service; search availability depends on Redis and the embedding service.
- AI summary generation depends on external API availability; without `AiSettings.ApiKey`, summaries will fail at runtime.

## 15. Suggested Mental Model For Working In This Repo

If you are onboarding into this project, the fastest way to understand it is:

1. Start with `source/frontend/in-shop/src/App.tsx` and `src/components/AppRoutes.tsx`.
2. Then inspect `source/backend/InShop.WebAPI/InShop.WebAPI/Program.cs`.
3. Follow session/cart flow through `api/client.ts`, `UserSessionController`, `SessionMiddleware`, `OrderController`, and frontend session/cart files.
4. Follow search flow through `useProductSearch.ts`, `SearchController.cs`, `VectorIndexingService.cs`, and `SearchResultsPage.tsx` (including recommendations Swiper).
5. Follow payment flow through `OrderSuccessPage.js`, `PaymentConfirmationPage.js`, `PaymentController.cs`, and `WebhookController.cs` (mock: `PaymentPage.js` + `PaymentsAPI`).
6. Follow reviews through `ProductPage.tsx`, `api/reviews.ts`, `ProductsController` review actions, and `ReviewService.cs`.
7. Follow AI summaries through `AiSummaryBlock.tsx`, `GetReviewAiSummary`, `ReviewCacheService`, and `AiAnalysisService.cs`.

## 16. Short Summary

`InShop` is a multi-service online store built around a .NET 8 API, SQL Server persistence, a React frontend, Redis-based hybrid search, and auxiliary payment/embedding services. Session-based carts work without full user accounts. Product reviews support voting, verified-purchase badges, and optional Yandex GPT summaries cached in Redis. Search combines lexical filtering with vector similarity and returns a separate recommendations carousel on the search results page.

## 17. Payment: YooKassa Integration

The main API can accept payments through **YooKassa** (test mode) or the legacy **PaymentsAPI** mock. The active provider is selected in configuration — no code changes required to switch.

### Switching providers

In `appsettings.json` or `appsettings.Development.json`:

```json
"Payment": {
  "Provider": "Mock"
}
```

or:

```json
"Payment": {
  "Provider": "YooKassa",
  "YooKassa": {
    "ShopId": "<shop_id>",
    "SecretKey": "<secret_key>",
    "BaseUrl": "https://api.yookassa.ru/v3",
    "WebhookSecret": "whsec_test",
    "IsTestMode": true,
    "ReturnUrl": "http://localhost:3000/payment-confirmation",
    "NotificationUrl": "https://<tunnel-host>/api/webhook/yookassa"
  }
}
```

| Value | Behavior |
|-------|----------|
| `Mock` (default in `appsettings.json`) | Card data sent to standalone `PaymentsAPI`; order status updated via `POST /api/webhooks/payment-confirmation`. |
| `YooKassa` | Creates payment in YooKassa API; frontend redirects to `confirmation_url`; order status updated via `POST /api/webhook/yookassa`. |

Secrets are read via `IConfiguration` (`config["Payment:YooKassa:ShopId"]`, etc.) — no `IOptions<T>`.

DI registration is conditional in `PaymentServiceExtensions.cs` (called from `Program.cs`): `YooKassaClient` and `YooKassaPaymentService` are registered only when `Provider == "YooKassa"`.

### Configuration checklist

1. **ShopId / SecretKey** — from YooKassa Merchant Profile (demo store for testing).
2. **ReturnUrl** — where the user returns after payment (must match a frontend route; project uses `/payment-confirmation?orderId=...`). Use **`http://`** for local React (`npm start`); `https://localhost:3000` causes a browser connection error because the dev server has no TLS certificate.
3. **NotificationUrl** — public HTTPS URL for webhooks (local dev: tunnel e.g. ngrok/localtunnel → `https://<tunnel>/api/webhook/yookassa`). Register the same URL in the YooKassa dashboard.
4. **PaymentsApi:BaseUrl** — still required when `Provider` is `Mock`.

### Flow (YooKassa)

1. Checkout → `/order-success` → «Оплатить» → `POST /api/Payment/initiate`.
2. Backend: `POST /v3/payments`, `metadata.order_id`, `Orders.YooKassaPaymentId` saved in SQL.
3. Redirect to YooKassa `confirmation_url`.
4. Return to `ReturnUrl` (`http://localhost:3000/payment-confirmation?orderId=...`).
5. `POST /api/Payment/confirm-yookassa` — запрос статуса в ЮKassa, при `succeeded` → `OrderStatus` и `PayStatus` = `Payed`.
6. Webhook `POST /api/webhook/yookassa` — дублирует шаг 5, когда туннель настроен.

**DB-first (physical database first):**

1. Run `source/backend/InShop.WebAPI/init/add_yookassa_payment_id.sql` on `InShopDB`.
2. C# mapping lives in partials: `Order.Payment.partial.cs`, `AppDbContext.Payment.partial.cs` (see `init/DB-FIRST-YOOKASSA.md`).
3. Optional: reverse scaffold table `Orders` after SQL change, then remove duplicate partial if property appears in `Order.cs`.

### Testing

**Test cards** (YooKassa docs): e.g. `5555555555554444`, any future expiry, any CVC.

**Webhook manually (curl):**

```bash
curl -X POST https://localhost:7275/api/webhook/yookassa \
  -H "Content-Type: application/json" \
  -d "{\"event\":\"payment.succeeded\",\"object\":{\"id\":\"test-payment-id\",\"status\":\"succeeded\",\"metadata\":{\"order_id\":\"1\"}}}"
```

Replace `order_id` with a real unpaid order id. Endpoint must return **HTTP 200** or YooKassa will retry delivery.

**Tunnel:** expose the main API (not PaymentsAPI) for `NotificationUrl` when testing real webhooks from YooKassa servers.

### API endpoints (YooKassa)

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/Payment/provider` | `Mock` or `YooKassa` for frontend routing |
| POST | `/api/Payment/initiate` | Create payment, return `redirectUrl` (session + `orderId` only) |
| POST | `/api/Payment/confirm-yookassa` | After return_url: sync `Payed` via YooKassa API + `YooKassaPaymentId` from DB |
| GET | `/api/Payment/status/{orderId}` | Order status for UI polling |
| POST | `/api/webhook/yookassa` | Webhook `payment.succeeded` |

### Key files

| File | Role |
|------|------|
| `InShop.WebAPI/Services/Payment/Clients/YooKassaClient.cs` | HTTP client, Basic auth, create/get payment |
| `InShop.WebAPI/Services/Payment/YooKassaPaymentService.cs` | Initiate, confirm, webhook, `TryMarkOrderAsPaid` |
| `InShopDbModels/Models/Order.cs` | `YooKassaPaymentId` column |
| `InShop.WebAPI/Controllers/PaymentController.cs` | `provider`, `initiate`, `confirm-yookassa` |
| `InShop.WebAPI/Controllers/WebhookController.cs` | `POST /api/webhook/yookassa` |
| `OrderSuccessPage.js` | Start YooKassa payment (no card form) |
| `PaymentConfirmationPage.js` | confirm + limited polling |
| `PaymentPage.js` | Mock only; redirects away when provider is YooKassa |

### Not implemented yet

- Webhook signature validation (`// TODO` in `YooKassaPaymentService`) — required for production.
- Refunds, partial capture, receipts (54-FZ).
- Saving payment methods / recurring payments.
