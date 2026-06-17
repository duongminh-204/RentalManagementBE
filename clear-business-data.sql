USE [RentalManagement];
GO

-- Clears business/sample data so the app starts with an empty workspace.
-- Keeps Users and Roles for login/authentication.

IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NOT NULL DELETE FROM [dbo].[Payments];
IF OBJECT_ID(N'[dbo].[InvoiceDetails]', N'U') IS NOT NULL DELETE FROM [dbo].[InvoiceDetails];
IF OBJECT_ID(N'[dbo].[Invoices]', N'U') IS NOT NULL DELETE FROM [dbo].[Invoices];
IF OBJECT_ID(N'[dbo].[UtilityUsages]', N'U') IS NOT NULL DELETE FROM [dbo].[UtilityUsages];
IF OBJECT_ID(N'[dbo].[Notifications]', N'U') IS NOT NULL DELETE FROM [dbo].[Notifications];
IF OBJECT_ID(N'[dbo].[Vehicles]', N'U') IS NOT NULL DELETE FROM [dbo].[Vehicles];
IF OBJECT_ID(N'[dbo].[RoomServices]', N'U') IS NOT NULL DELETE FROM [dbo].[RoomServices];
IF OBJECT_ID(N'[dbo].[Devices]', N'U') IS NOT NULL DELETE FROM [dbo].[Devices];
IF OBJECT_ID(N'[dbo].[RoomImages]', N'U') IS NOT NULL DELETE FROM [dbo].[RoomImages];
IF OBJECT_ID(N'[dbo].[Contracts]', N'U') IS NOT NULL DELETE FROM [dbo].[Contracts];
IF OBJECT_ID(N'[dbo].[Tenants]', N'U') IS NOT NULL DELETE FROM [dbo].[Tenants];
IF OBJECT_ID(N'[dbo].[Expenses]', N'U') IS NOT NULL DELETE FROM [dbo].[Expenses];
IF OBJECT_ID(N'[dbo].[Posts]', N'U') IS NOT NULL DELETE FROM [dbo].[Posts];
IF OBJECT_ID(N'[dbo].[Rooms]', N'U') IS NOT NULL DELETE FROM [dbo].[Rooms];
IF OBJECT_ID(N'[dbo].[Buildings]', N'U') IS NOT NULL DELETE FROM [dbo].[Buildings];
IF OBJECT_ID(N'[dbo].[Services]', N'U') IS NOT NULL DELETE FROM [dbo].[Services];
IF OBJECT_ID(N'[dbo].[DeviceCatalogs]', N'U') IS NOT NULL DELETE FROM [dbo].[DeviceCatalogs];
GO

DECLARE @table sysname;
DECLARE reset_cursor CURSOR FOR
SELECT [name]
FROM sys.tables
WHERE [name] IN (
    N'Payments',
    N'InvoiceDetails',
    N'Invoices',
    N'UtilityUsages',
    N'Notifications',
    N'Vehicles',
    N'RoomServices',
    N'Devices',
    N'RoomImages',
    N'Contracts',
    N'Tenants',
    N'Expenses',
    N'Posts',
    N'Rooms',
    N'Buildings',
    N'Services',
    N'DeviceCatalogs'
)
AND OBJECTPROPERTY([object_id], 'TableHasIdentity') = 1;

OPEN reset_cursor;
FETCH NEXT FROM reset_cursor INTO @table;

WHILE @@FETCH_STATUS = 0
BEGIN
    DBCC CHECKIDENT (@table, RESEED, 0);
    FETCH NEXT FROM reset_cursor INTO @table;
END

CLOSE reset_cursor;
DEALLOCATE reset_cursor;
GO
