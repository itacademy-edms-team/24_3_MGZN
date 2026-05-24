/*
================================================================================
  InShop — Identity (админ JWT) + OrderAuditLog + расширение ImageURL
  Файл: source/backend/InShop.WebAPI/scripts/AddAdminIdentityAndAudit.sql
================================================================================

  ЧТО СДЕЛАТЬ:
  1. Backup базы InShopDB.
  2. SSMS → выбрать InShopDB → выполнить скрипт (F5).
  3. СНАЧАЛА выполните scripts/CreateAspNetIdentityTables.sql (AspNetUsers и др.).
  4. Затем этот скрипт (OrderAuditLog + ImageURL).
  5. Перезапустить API.

  Подробно: scripts/SCAFFOLDING.md
================================================================================
*/

SET NOCOUNT ON;

-- Расширение пути к изображению товара для /uploads/products/...
IF COL_LENGTH(N'dbo.Products', N'ImageURL') IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX);
    -- Увеличиваем длину столбца, если текущая меньше 500
    SET @sql = N'
    ALTER TABLE dbo.Products ALTER COLUMN ImageURL NVARCHAR(500) NULL;';
    BEGIN TRY
        EXEC sp_executesql @sql;
        PRINT N'ImageURL расширен до NVARCHAR(500).';
    END TRY
    BEGIN CATCH
        PRINT N'ImageURL: ' + ERROR_MESSAGE();
    END CATCH
END

IF OBJECT_ID(N'dbo.OrderAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderAuditLogs (
        AuditId     BIGINT IDENTITY(1,1) NOT NULL,
        OrderId     INT NOT NULL,
        OldStatus   NVARCHAR(50) NULL,
        NewStatus   NVARCHAR(50) NOT NULL,
        ChangedBy   NVARCHAR(256) NOT NULL,
        CreatedAt   DATETIME NOT NULL CONSTRAINT DF_OrderAuditLogs_CreatedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_OrderAuditLogs PRIMARY KEY (AuditId),
        CONSTRAINT FK_OrderAuditLogs_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE
    );
    CREATE INDEX IX_OrderAuditLogs_OrderId ON dbo.OrderAuditLogs(OrderId);
    PRINT N'Создана таблица OrderAuditLogs.';
END
ELSE
    PRINT N'OrderAuditLogs уже существует.';

PRINT N'Identity: см. scripts/CreateAspNetIdentityTables.sql';
