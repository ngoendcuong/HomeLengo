-- Script để xóa bảng ServiceRegisters cũ và tạo bảng UserServicePackages mới
-- Chạy script này trong SQL Server Management Studio hoặc công cụ quản lý database của bạn

-- Bước 1: Xóa Foreign Key constraints liên quan đến ServiceRegisters (nếu có)
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ServiceRegisters_User')
BEGIN
    ALTER TABLE [dbo].[ServiceRegisters] DROP CONSTRAINT [FK_ServiceRegisters_User];
    PRINT 'Đã xóa Foreign Key FK_ServiceRegisters_User';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ServiceRegisters_Plan')
BEGIN
    ALTER TABLE [dbo].[ServiceRegisters] DROP CONSTRAINT [FK_ServiceRegisters_Plan];
    PRINT 'Đã xóa Foreign Key FK_ServiceRegisters_Plan';
END

-- Bước 2: Xóa bảng ServiceRegisters (nếu có dữ liệu quan trọng, hãy backup trước)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceRegisters')
BEGIN
    DROP TABLE [dbo].[ServiceRegisters];
    PRINT 'Đã xóa bảng ServiceRegisters';
END

-- Bước 3: Tạo bảng UserServicePackages mới
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserServicePackages')
BEGIN
    CREATE TABLE [dbo].[UserServicePackages] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [PlanId] INT NOT NULL,
        [StartDate] DATETIME NOT NULL DEFAULT (getdate()),
        [EndDate] DATETIME NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME NOT NULL DEFAULT (getdate()),
        CONSTRAINT [PK__UserServicePackages__Id] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserServicePackages_User] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[Users]([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserServicePackages_Plan] FOREIGN KEY ([PlanId]) 
            REFERENCES [dbo].[ServicePlans]([PlanId]) ON DELETE NO ACTION
    );
    
    -- Tạo index để tăng hiệu suất query
    CREATE INDEX [IX_UserServicePackages_UserId] ON [dbo].[UserServicePackages]([UserId]);
    CREATE INDEX [IX_UserServicePackages_IsActive] ON [dbo].[UserServicePackages]([IsActive]);
    
    PRINT 'Đã tạo bảng UserServicePackages thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng UserServicePackages đã tồn tại!';
END

-- Bước 4: Thêm cột MaxListings vào bảng ServicePlans (nếu chưa có)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ServicePlans]') AND name = 'MaxListings')
BEGIN
    ALTER TABLE [dbo].[ServicePlans]
    ADD [MaxListings] INT NULL;
    PRINT 'Đã thêm cột MaxListings vào bảng ServicePlans';
END

PRINT 'Hoàn tất cập nhật database!';
PRINT 'Bảng UserServicePackages đã sẵn sàng sử dụng với các trường:';
PRINT '  - Id: Primary Key';
PRINT '  - UserId: Foreign Key đến Users';
PRINT '  - PlanId: Foreign Key đến ServicePlans';
PRINT '  - StartDate: Ngày bắt đầu mua';
PRINT '  - EndDate: Ngày kết thúc (nullable)';
PRINT '  - IsActive: Trạng thái active';
PRINT '  - CreatedAt: Ngày tạo';


