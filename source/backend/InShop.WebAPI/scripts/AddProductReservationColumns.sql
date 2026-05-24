/*
================================================================================
  InShop — миграция таблицы Products для резервирования остатков
  Файл: source/backend/InShop.WebAPI/scripts/AddProductReservationColumns.sql
================================================================================

  ЧТО НУЖНО СДЕЛАТЬ (один раз на каждой базе InShopDB):

  1. Сделайте резервную копию базы (Backup), если это не локальная dev-среда.

  2. Откройте SQL Server Management Studio (SSMS) или Azure Data Studio.

  3. Подключитесь к серверу из ConnectionStrings:DefaultConnection
     (см. appsettings.json, например Server=...;Database=InShopDB).

  4. Выберите базу InShopDB и выполните ВЕСЬ этот скрипт (F5).

  5. Убедитесь, что скрипт завершился без ошибок. Проверка:

       SELECT TOP 5
           ProductId,
           ProductStockQuantity,
           ReservedQuantity,
           RowVersion
       FROM dbo.Products;

     — колонки ReservedQuantity (0 для старых строк) и RowVersion должны быть заполнены.

  6. Перезапустите ASP.NET Core API. Модель Product уже содержит новые поля в коде.

  7. НЕ перегенерируйте scaffold EF из БД без сохранения полей ReservedQuantity и RowVersion
     в partial-классе Product (см. PROJECT_CONTEXT.md, раздел Inventory & Reservation System).

  Альтернатива (если позже включите Code First migrations):

    cd source/backend/InShop.WebAPI/InShop.WebAPI
    dotnet ef migrations add AddProductReservation --project ..\InShopDbModels\InShopDbModels.csproj --startup-project .
    dotnet ef database update

  Для текущего репозитория (database-first) достаточно выполнить этот SQL-скрипт.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- Свободный остаток уже хранится в ProductStockQuantity; добавляем только резерв и rowversion.

IF COL_LENGTH(N'dbo.Products', N'ReservedQuantity') IS NULL
BEGIN
    ALTER TABLE dbo.Products
        ADD ReservedQuantity INT NOT NULL
            CONSTRAINT DF_Products_ReservedQuantity DEFAULT (0);

    PRINT N'Добавлена колонка ReservedQuantity.';
END
ELSE
    PRINT N'Колонка ReservedQuantity уже существует — пропуск.';

IF COL_LENGTH(N'dbo.Products', N'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Products
        ADD [RowVersion] rowversion NOT NULL;

    PRINT N'Добавлена колонка RowVersion (rowversion).';
END
ELSE
    PRINT N'Колонка RowVersion уже существует — пропуск.';

-- Для строк, созданных до миграции: резерв = 0 (DEFAULT уже применён при ADD).

COMMIT TRANSACTION;

PRINT N'Миграция AddProductReservationColumns завершена успешно.';
