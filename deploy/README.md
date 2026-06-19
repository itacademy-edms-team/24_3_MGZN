# InShop Docker Deploy

This folder contains Docker Compose files for running the InShop stack locally and later on a server.

## Services

- `frontend` - React app served by nginx
- `inshop-api` - ASP.NET Core WebAPI
- `sqlserver` - SQL Server 2025
- `redis` - cache/search infrastructure
- `embedding-server` - FastAPI embeddings service

`PaymentsAPI` is intentionally not part of the main compose file. It is a development-only mock payment service and is available through `docker-compose.dev.yml`.

## First Run

1. Copy the env template:

   ```powershell
   Copy-Item .env.example .env
   ```

2. Open `.env` and replace passwords and API keys.
   The main compose file is production-like and defaults to `PAYMENT_PROVIDER=YooKassa`,
   so `YOOKASSA_*` values must be set explicitly. For local mock payments, use the
   development overlay below.

3. Start the main stack:

   ```powershell
   docker compose up --build
   ```

4. Open:

   - Frontend: `http://localhost:3000`
   - API: `http://localhost:5000`

The first `embedding-server` build/download can take a long time because the LaBSE model is large.

## Database Bootstrap

The repository currently uses a database-first EF model and does not have EF migrations. For Docker, `inshop-api` runs with:

```env
Database__EnsureCreated=true
```

This creates the business schema from `AppDbContext` and creates ASP.NET Identity tables from `AdminIdentityDbContext` when the Docker database is empty. The API then seeds the `Admin` role at startup.

For a real production migration strategy, add EF migrations or a full idempotent SQL schema script before relying on this for long-term database evolution.

## Development Mock Payments

To include the mock `PaymentsAPI` and switch the main API to `Payment__Provider=Mock`, run:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

The main compose file stays production-like and uses `YooKassa`.

## SQL Server Version

The SQL Server image is pinned to SQL Server 2025 by digest because the local development
backup was created by a newer SQL Server database engine and cannot be restored into SQL
Server 2022. Keep `sqlserver` and `sqlserver-init` on the same image. If you intentionally
upgrade SQL Server later, refresh the digest in `docker-compose.yml` and recreate the SQL
container after taking a backup.

## Useful Commands

Stop containers:

```powershell
docker compose down
```

Stop and delete volumes:

```powershell
docker compose down -v
```

View API logs:

```powershell
docker compose logs -f inshop-api
```

Validate compose configuration:

```powershell
docker compose config
```
