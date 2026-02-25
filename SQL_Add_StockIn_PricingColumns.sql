-- Run once on your POS LocalDB
-- Adds pricing history fields to tbStockIn so each stock-in row stores its own CostPrice/Markup/Price

IF COL_LENGTH('dbo.tbStockIn', 'CostPrice') IS NULL
    ALTER TABLE dbo.tbStockIn ADD CostPrice DECIMAL(18,2) NULL;

IF COL_LENGTH('dbo.tbStockIn', 'Markup') IS NULL
    ALTER TABLE dbo.tbStockIn ADD Markup DECIMAL(18,2) NULL;

IF COL_LENGTH('dbo.tbStockIn', 'Price') IS NULL
    ALTER TABLE dbo.tbStockIn ADD Price DECIMAL(18,2) NULL;
