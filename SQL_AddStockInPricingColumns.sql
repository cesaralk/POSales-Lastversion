/*
Adds pricing history columns to tbStockIn.

Why:
- Stock In screen can edit CostPrice/Markup/Price per stock entry.
- Values are stored on tbStockIn (history) and DO NOT overwrite tbProduct.

Run this once on your LocalDB database.
*/

IF COL_LENGTH('dbo.tbStockIn', 'CostPrice') IS NULL
    ALTER TABLE dbo.tbStockIn ADD CostPrice DECIMAL(18,2) NULL;

IF COL_LENGTH('dbo.tbStockIn', 'Markup') IS NULL
    ALTER TABLE dbo.tbStockIn ADD Markup DECIMAL(18,2) NULL;

IF COL_LENGTH('dbo.tbStockIn', 'Price') IS NULL
    ALTER TABLE dbo.tbStockIn ADD Price DECIMAL(18,2) NULL;
GO
