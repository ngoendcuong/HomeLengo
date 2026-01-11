-- Script để thêm các cột mới vào database
-- Chạy script này trong SQL Server Management Studio hoặc công cụ quản lý database của bạn

-- Thêm cột UserId vào bảng ServiceRegisters
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ServiceRegisters]') AND name = 'UserId')
BEGIN
    ALTER TABLE [dbo].[ServiceRegisters]
    ADD [UserId] INT NULL;
    
    -- Cập nhật UserId từ Email (nếu có dữ liệu cũ)
    UPDATE sr
    SET sr.UserId = u.UserId
    FROM [dbo].[ServiceRegisters] sr
    INNER JOIN [dbo].[Users] u ON sr.Email = u.Email
    WHERE sr.UserId IS NULL;
    
    -- Đặt NOT NULL sau khi cập nhật dữ liệu
    ALTER TABLE [dbo].[ServiceRegisters]
    ALTER COLUMN [UserId] INT NOT NULL;
    
    -- Tạo Foreign Key
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ServiceRegisters_User')
    BEGIN
        ALTER TABLE [dbo].[ServiceRegisters]
        ADD CONSTRAINT [FK_ServiceRegisters_User] 
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId])
        ON DELETE NO ACTION;
    END
    
    PRINT 'Đã thêm cột UserId và Foreign Key thành công!';
END

-- Thêm cột IsPaid và PaidAt vào bảng ServiceRegisters
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ServiceRegisters]') AND name = 'IsPaid')
BEGIN
    ALTER TABLE [dbo].[ServiceRegisters]
    ADD [IsPaid] BIT NOT NULL DEFAULT 0;
    PRINT 'Đã thêm cột IsPaid thành công!';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ServiceRegisters]') AND name = 'PaidAt')
BEGIN
    ALTER TABLE [dbo].[ServiceRegisters]
    ADD [PaidAt] DATETIME NULL;
    PRINT 'Đã thêm cột PaidAt thành công!';
END

-- Thêm cột MaxListings vào bảng ServicePlans
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ServicePlans]') AND name = 'MaxListings')
BEGIN
    ALTER TABLE [dbo].[ServicePlans]
    ADD [MaxListings] INT NULL;
    PRINT 'Đã thêm cột MaxListings thành công!';
END

PRINT 'Hoàn tất cập nhật database!';

