/*
================================================================================
  InShop — расширение Categories.ImageURL для загрузок /uploads/categories/...
  Файл: source/backend/InShop.WebAPI/scripts/WidenCategoryImageUrl.sql
================================================================================

  ЧТО СДЕЛАТЬ:
  1. Backup базы InShopDB.
  2. SSMS → выбрать InShopDB → выполнить скрипт (F5).
  3. Перезапустить API.
================================================================================
*/

SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.Categories', N'ImageURL') IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX);
    SET @sql = N'
    ALTER TABLE dbo.Categories ALTER COLUMN ImageURL NVARCHAR(500) NULL;';
    BEGIN TRY
        EXEC sp_executesql @sql;
        PRINT N'Categories.ImageURL расширен до NVARCHAR(500).';
    END TRY
    BEGIN CATCH
        PRINT N'Categories.ImageURL: ' + ERROR_MESSAGE();
    END CATCH
END
ELSE
    PRINT N'Столбец Categories.ImageURL не найден.';
