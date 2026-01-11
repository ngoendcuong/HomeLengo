-- Script kiểm tra và tạo bảng ContactUs nếu chưa có
-- Chạy script này trong SQL Server Management Studio

-- Kiểm tra xem bảng có tồn tại không
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContactUs')
BEGIN
    PRINT 'Bảng ContactUs không tồn tại. Đang tạo bảng...';
    
    CREATE TABLE [dbo].[ContactUs] (
        [ContactId] INT IDENTITY(1,1) NOT NULL,
        [FullName] NVARCHAR(150) NOT NULL,
        [Email] NVARCHAR(150) NOT NULL,
        [Phone] NVARCHAR(20) NULL,
        [Information] NVARCHAR(200) NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [Status] NVARCHAR(50) NULL DEFAULT 'New',
        [CreatedAt] DATETIME NULL DEFAULT (getdate()),
        CONSTRAINT [PK__ContactU__5C66259B6EFE14F6] PRIMARY KEY ([ContactId])
    );
    
    PRINT 'Đã tạo bảng ContactUs thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng ContactUs đã tồn tại.';
    
    -- Kiểm tra các cột
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ContactUs]') AND name = 'ContactId')
    BEGIN
        PRINT 'Thiếu cột ContactId';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ContactUs]') AND name = 'FullName')
    BEGIN
        PRINT 'Thiếu cột FullName';
        ALTER TABLE [dbo].[ContactUs] ADD [FullName] NVARCHAR(150) NOT NULL DEFAULT '';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ContactUs]') AND name = 'Email')
    BEGIN
        PRINT 'Thiếu cột Email';
        ALTER TABLE [dbo].[ContactUs] ADD [Email] NVARCHAR(150) NOT NULL DEFAULT '';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ContactUs]') AND name = 'Phone')
    BEGIN
        PRINT 'Thiếu cột Phone';
        ALTER TABLE [dbo].[ContactUs] ADD [Phone] NVARCHAR(20) NULL;
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ContactUs]') AND name = 'Information')
    BEGIN
        PRINT 'Thiếu cột Information';
        ALTER TABLE [dbo].[ContactUs] ADD [Information] NVARCHAR(200) NULL;
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ContactUs]') AND name = 'Message')
    BEGIN
        PRINT 'Thiếu cột Message';
        ALTER TABLE [dbo].[ContactUs] ADD [Message] NVARCHAR(MAX) NOT NULL DEFAULT '';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ContactUs]') AND name = 'Status')
    BEGIN
        PRINT 'Thiếu cột Status';
        ALTER TABLE [dbo].[ContactUs] ADD [Status] NVARCHAR(50) NULL DEFAULT 'New';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ContactUs]') AND name = 'CreatedAt')
    BEGIN
        PRINT 'Thiếu cột CreatedAt';
        ALTER TABLE [dbo].[ContactUs] ADD [CreatedAt] DATETIME NULL DEFAULT (getdate());
    END
END

-- Kiểm tra dữ liệu mẫu
SELECT TOP 5 * FROM [dbo].[ContactUs] ORDER BY [CreatedAt] DESC;

PRINT 'Hoàn tất kiểm tra bảng ContactUs!';


