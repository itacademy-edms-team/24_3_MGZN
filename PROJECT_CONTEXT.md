# InShop Project Context

## 1. What This Repository Contains

`InShop` is an e-commerce system with:

- a primary backend on ASP.NET Core Web API
- a frontend on React
- a separate payment simulation service
- a Python embedding service for semantic/vector search

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
- `StackExchange.Redis`
- `Swashbuckle / Swagger`
- `RazorLight` for email templates

Key backend project files:

- `source/backend/InShop.WebAPI/InShop.WebAPI/InShop.WebAPI.csproj`
- `source/backend/InShop.WebAPI/InShopBLLayer/InShopBLLayer.csproj`
- `source/backend/InShop.WebAPI/InShopDbModels/InShopDbModels.csproj`

### Frontend

- `React 19`
- `React Router`
- `Axios`
- `TypeScript` + `JavaScript` mixed codebase
- `Create React App` / `react-scripts`

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
      -> Redis (vector + lexical search index)
      -> PaymentProcessingService
          -> PaymentsAPI
              -> webhook callback
                  -> Main Web API updates order status

Vector indexing/search:
SQL Server products
  -> Background indexing service
      -> Python EmbeddingServer
      -> Redis index
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
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/ProductsController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/CategoryController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/OrderController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/UserSessionController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/SearchController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/PaymentController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/WebhookController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/VerificationController.cs`

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
- reviews
- vector indexing

Important file:

- `source/backend/InShop.WebAPI/InShopBLLayer/Extensions/ServiceCollectionBLLayerExtension.cs`

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

#### `Contracts`

Role: DTOs shared across API and service boundaries.

Examples:

- search DTOs
- order DTOs
- payment DTOs
- review DTOs
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
- `SearchController` performs hybrid search
- `PaymentController` starts external payment processing
- `WebhookController` receives payment result callbacks

### Business layer

Responsibilities:

- domain/business workflows
- orchestration across repositories
- mapping entities to DTOs
- cache/search background processes
- email and verification logic

### Data layer

Responsibilities:

- EF Core context
- repository implementations
- entity persistence
- SQL Server interaction

## 6. Domain Model

Based on `AppDbContext`, the main domain entities are:

- `Product`
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
- product reviews with voting
- structured product specifications used in filtering/search

## 7. Frontend Structure

Frontend root:

- `source/frontend/in-shop`

Main entry points:

- `source/frontend/in-shop/src/index.js`
- `source/frontend/in-shop/src/App.tsx`
- `source/frontend/in-shop/src/components/AppRoutes.tsx`

### Frontend responsibilities

#### Application shell

- `App.tsx` wires routing, session provider, session initialization, cart provider, header/footer, and modal cart UI.

#### Routing

Routes currently include:

- catalog
- category page
- product page
- search page
- checkout
- payment
- payment confirmation
- order success
- email verification

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
- axios interceptor recreates the session on `401`

#### Cart state

Main file:

- `source/frontend/in-shop/src/components/CartContext.js`

Behavior:

- cart operations go through backend order endpoints
- session token is not sent manually; backend reads it from cookie
- cart is fetched lazily and revalidated when needed

#### Search UI

Main files:

- `source/frontend/in-shop/src/hooks/useProductSearch.ts`
- `source/frontend/in-shop/src/pages/SearchResultPage/SearchResultsPage.tsx`
- `source/frontend/in-shop/src/components/SearchComponent/SearchComponent.tsx`
- `source/frontend/in-shop/src/components/FiltersPanel/FiltersPanel.tsx`
- `source/frontend/in-shop/src/components/SortMenu/SortMenu.tsx`

Behavior:

- frontend sends POST requests to search API
- supports pagination, sorting, stock filter, price filters, category filter, and spec filters
- keeps separate result and recommendation lists

## 8. Core Runtime Flows

### 8.1 Session lifecycle

1. Frontend initializes through `SessionHandler`.
2. `useSession` checks whether a session is active.
3. If no valid session exists, frontend calls `POST /api/UserSession`.
4. Backend creates a `UserSession`, creates a draft order, and writes a secure `SessionToken` cookie.
5. Future requests rely on the cookie and middleware/controller validation.

Key files:

- `source/frontend/in-shop/src/hooks/useSession.ts`
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

1. Frontend payment page posts card data and `orderId` to main API.
2. Main API validates that the order exists and is unpaid.
3. `PaymentProcessingService` calls external `PaymentsAPI`.
4. `PaymentsAPI` simulates payment delay and sends webhook to main API.
5. Main API webhook updates order status.
6. Frontend can query payment status.

Key files:

- `source/frontend/in-shop/src/pages/PaymentPage.js`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/PaymentController.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Services/PaymentProcessingService.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/WebhookController.cs`
- `source/backend/PaymentsAPI/PaymentsAPI/Controllers/PaymentController.cs`

### 8.4 Search flow

1. `VectorIndexingService` loads products and specifications from SQL Server.
2. Product text is assembled from category, title, description, and specs.
3. Text is sent to the Python embedding service.
4. Embeddings and searchable metadata are stored in Redis hashes.
5. Redis index `idx:products` is built/updated.
6. `SearchController` runs lexical and vector searches in Redis and merges scores into hybrid search results.
7. Recommendations are generated from a broader result set.

Key files:

- `source/backend/InShop.WebAPI/InShopBLLayer/Services/Search/VectorIndexingService.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Controllers/SearchController.cs`
- `source/backend/EmbeddingServer/server.py`

## 9. Search Architecture Details

The project contains a relatively advanced search subsystem:

- vector embeddings generated externally by Python
- Redis used as a hybrid search engine
- product specs included in index schema
- hybrid score combines lexical and vector scores
- filters support category, price, stock, and dynamic specification fields

Important implementation notes:

- vector dimension is `768`
- Redis key prefix is `product:`
- main index name is `idx:products`
- index rebuild is performed by a hosted background service
- search recommendations are separate from the main paged result list

## 10. Configuration and Entry Points

### Main backend

- `source/backend/InShop.WebAPI/InShop.WebAPI/Program.cs`
- `source/backend/InShop.WebAPI/InShop.WebAPI/appsettings.json`
- `source/backend/InShop.WebAPI/InShop.WebAPI/appsettings.Development.json`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Properties/launchSettings.json`

### Payment simulator

- `source/backend/PaymentsAPI/PaymentsAPI/Program.cs`
- `source/backend/PaymentsAPI/PaymentsAPI/appsettings.Development.json`
- `source/backend/PaymentsAPI/PaymentsAPI/Properties/launchSettings.json`

### Frontend

- `source/frontend/in-shop/package.json`
- `source/frontend/in-shop/public/index.html`
- `source/frontend/in-shop/src/index.js`

### Docker

- `source/backend/InShop.WebAPI/docker-compose.yml`
- `source/backend/InShop.WebAPI/InShop.WebAPI/Dockerfile`

## 11. Local Infrastructure Dependencies

The code expects these local services/components:

- SQL Server
- Redis on `localhost:6379`
- Embedding server on `http://localhost:8000`
- frontend dev server on `http://localhost:3000`
- main backend API on `https://localhost:7275` in current frontend code
- payment service on its configured local URL

## 12. Testing Situation

Detected automated tests:

- Cypress E2E tests in `source/frontend/in-shop/cypress-tests/cypress/e2e`

Examples:

- product browsing
- cart management
- checkout validation

I did not detect a dedicated backend unit/integration test project in the repository snapshot I analyzed.

## 13. Architectural Strengths

- clear backend layering: API -> BLL -> repositories
- explicit DTO/Contracts project
- session/cart flow separated from full user-auth complexity
- advanced hybrid search design using structured specs + embeddings
- payment flow isolated behind a separate service boundary
- frontend session bootstrapping is centralized

## 14. Architectural Risks / Technical Observations

These are important context notes for future development:

- The repository contains multiple local/runtime configuration points that appear environment-specific.
- `AppDbContext` still contains a hardcoded SQL Server connection string in `OnConfiguring`, even though DI also supplies a connection string.
- `Program.cs` in the main API defines one CORS policy but also calls `UseCors("AllowFrontend")`, which does not match the visible registered policy name and may indicate config drift.
- The frontend is a mixed JS/TS codebase, which increases maintenance cost and type inconsistency risk.
- Payment simulation currently logs raw card information in the standalone `PaymentsAPI`, which is acceptable only for local/demo use and should never remain in a production-like environment.
- The payment flow is asynchronous and webhook-driven, so order state consistency depends on both services being available.
- The vector index is periodically rebuilt by a background service; search availability/accuracy depends on Redis and the embedding service being healthy.

## 15. Suggested Mental Model For Working In This Repo

If you are onboarding into this project, the fastest way to understand it is:

1. Start with `source/frontend/in-shop/src/App.tsx` and `src/components/AppRoutes.tsx`.
2. Then inspect `source/backend/InShop.WebAPI/InShop.WebAPI/Program.cs`.
3. Follow session/cart flow through `UserSessionController`, `SessionMiddleware`, `OrderController`, and frontend session/cart files.
4. Follow search flow through `useProductSearch.ts`, `SearchController.cs`, and `VectorIndexingService.cs`.
5. Follow payment flow through `PaymentPage.js`, main `PaymentController.cs`, standalone `PaymentsAPI`, and `WebhookController.cs`.

## 16. Short Summary

`InShop` is a multi-service online store application built around a .NET 8 API, SQL Server persistence, a React frontend, Redis-based hybrid search, and auxiliary payment/embedding services. Its most distinctive architectural features are session-based cart management without full user accounts and a hybrid search subsystem that combines lexical filtering with vector similarity over product metadata and specifications.
