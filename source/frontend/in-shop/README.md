# Frontend InShop

Frontend-часть интернет-магазина InShop. Приложение написано на React и собрано на Create React App (`react-scripts`).

## Основные команды

Все команды выполняются из папки `source/frontend/in-shop`.

### Установка зависимостей

```powershell
npm ci
```

Для локальной разработки можно использовать `npm install`, но в CI применяется именно `npm ci`, потому что он устанавливает зависимости строго по `package-lock.json`.

### Запуск в режиме разработки

```powershell
npm start
```

Приложение откроется на `http://localhost:3000`. При изменении файлов страница автоматически перезагружается.

### Production-сборка

```powershell
npm run build
```

Команда создаёт оптимизированную сборку в папке `build`.

### Проверки качества

```powershell
npm run typecheck
npm run test:ci
```

В GitHub Actions frontend job запускает `typecheck`, `test:ci` и `build`. Переменная `CI=true` включена автоматически, поэтому ESLint warnings считаются ошибками сборки. Если локально `npm run build` проходит, а в CI падает, проверьте предупреждения ESLint.

## API URL

Базовый URL API задаётся через `REACT_APP_API_BASE_URL`.

Если переменная не указана, frontend использует `/api`. Это удобно для Docker/nginx, где запросы проксируются из frontend-контейнера в `inshop-api`.

## Docker

Frontend image собирается через `source/frontend/in-shop/Dockerfile`.

В CI/CD image публикуется в GitHub Container Registry как:

- `ghcr.io/<owner>/inshop-frontend:latest` для `main`/`master`;
- `ghcr.io/<owner>/inshop-frontend:dev` для `dev`;
- `ghcr.io/<owner>/inshop-frontend:sha-<commit>` для каждой push-сборки.

## Cypress

E2E-тесты находятся в `source/frontend/in-shop/cypress-tests`.

Локальный запуск:

```powershell
cd cypress-tests
npm install
npm run cypress:open
```

Headless-запуск:

```powershell
npm run cypress:run
```

Из корня frontend-пакета можно запустить тот же headless-прогон командой:

```powershell
npm run cypress:run
```

## Tooling debt

Проект пока остаётся на Create React App (`react-scripts`). Миграцию на Vite/RSBuild лучше делать отдельным PR после security/runtime исправлений: потребуется проверить React Router, env-переменные, Docker build и Cypress запуск.
