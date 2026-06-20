# Docker-запуск InShop

Эта папка содержит Docker Compose-файлы для локального запуска стека InShop и будущего запуска на сервере.

## Сервисы

- `frontend` - React-приложение, которое отдаётся через nginx
- `inshop-api` - основной ASP.NET Core WebAPI
- `sqlserver` - SQL Server 2025
- `redis` - инфраструктура кеша и поиска
- `embedding-server` - FastAPI-сервис для генерации embeddings

`PaymentsAPI` намеренно не входит в основной compose-файл. Это mock-сервис оплаты только для разработки, он подключается через `docker-compose.dev.yml`.

## Первый запуск

1. Скопировать шаблон переменных окружения:

   ```powershell
   Copy-Item .env.example .env
   ```

2. Открыть `.env` и заменить пароли/API-ключи.
   Основной compose-файл настроен как production-like и по умолчанию использует `PAYMENT_PROVIDER=YooKassa`,
   поэтому значения `YOOKASSA_*` нужно указать явно. Для локальной mock-оплаты используйте dev-overlay ниже.

3. Запустить основной стек:

   ```powershell
   docker compose up --build
   ```

4. Открыть:

   - Frontend: `http://localhost:3000`
   - API: `http://localhost:5000`

Первая сборка/загрузка `embedding-server` может занять много времени, потому что модель LaBSE большая.

## Инициализация базы данных

Сейчас проект использует database-first EF-модель и не содержит EF migrations. Для Docker `inshop-api` запускается с настройкой:

```env
Database__EnsureCreated=true
```

Если Docker-база пустая, эта настройка создаёт бизнес-схему из `AppDbContext` и таблицы ASP.NET Identity из `AdminIdentityDbContext`. После этого API при старте добавляет роль `Admin`.

Для настоящей production-стратегии миграций нужно добавить EF migrations или полноценный idempotent SQL-скрипт схемы. Не стоит полагаться на `EnsureCreated` для долгосрочного развития production-БД.

## Mock-оплата для разработки

Чтобы подключить mock-сервис `PaymentsAPI` и переключить основной API на `Payment__Provider=Mock`, выполните:

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

Основной compose-файл остаётся production-like и использует `YooKassa`.

## CI/CD-образы

Workflow GitHub Actions `.github/workflows/ci.yml` собирает и тестирует приложение перед публикацией container images.

Для pull request Docker images только собираются как проверка Dockerfile и не публикуются в registry.

При push в `main`, `master` или `dev` workflow публикует эти images в GitHub Container Registry:

- `ghcr.io/<owner>/inshop-api`
- `ghcr.io/<owner>/inshop-frontend`
- `ghcr.io/<owner>/inshop-embedding-server`

Теги образов:

- `latest` публикуется только из `main` или `master`
- `dev` публикуется только из `dev`
- `sha-<commit>` публикуется для каждой push-сборки

`PaymentsAPI` не публикуется как production image. Он остаётся mock-сервисом только для разработки.

Production deployment намеренно не входит в текущий workflow. Отдельная будущая задача должна определить, где будет сервер, как он будет авторизоваться в GHCR, как будут храниться `.env`-значения и будет ли деплой ручным или автоматическим.

## Версия SQL Server

SQL Server image закреплён на SQL Server 2025
Держите `sqlserver` и `sqlserver-init` на одном и том же image. Если позже вы осознанно обновляете SQL Server, обновите digest в `docker-compose.yml` и пересоздайте SQL-контейнер после создания backup.

## Полезные команды

Остановить контейнеры:

```powershell
docker compose down
```

Остановить контейнеры и удалить volumes:

```powershell
docker compose down -v
```

Посмотреть логи API:

```powershell
docker compose logs -f inshop-api
```

Проверить compose-конфигурацию:

```powershell
docker compose config
```
