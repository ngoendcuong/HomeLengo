-- Script tạo các bảng bổ sung cho Property Details
-- Chạy script này trong SQL Server Management Studio

USE HomeLengo;
GO

-- 1. Bảng PropertyVideo - Lưu video của property
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PropertyVideo]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PropertyVideo] (
        [VideoId] INT IDENTITY(1,1) NOT NULL,
        [PropertyId] INT NOT NULL,
        [VideoUrl] NVARCHAR(1000) NOT NULL,
        [VideoType] NVARCHAR(50) NULL, -- 'youtube', 'vimeo', 'direct'
        [ThumbnailUrl] NVARCHAR(1000) NULL,
        [Title] NVARCHAR(255) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [SortOrder] INT NULL DEFAULT 0,
        [IsPrimary] BIT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_PropertyVideo] PRIMARY KEY CLUSTERED ([VideoId] ASC),
        CONSTRAINT [FK_PropertyVideo_Property] FOREIGN KEY ([PropertyId]) 
            REFERENCES [dbo].[Properties] ([PropertyId]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_PropertyVideo_PropertyId] ON [dbo].[PropertyVideo]([PropertyId]);
END
GO

-- 2. Bảng PropertyFloorPlan - Lưu floor plans (sơ đồ tầng)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PropertyFloorPlan]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PropertyFloorPlan] (
        [FloorPlanId] INT IDENTITY(1,1) NOT NULL,
        [PropertyId] INT NOT NULL,
        [FloorName] NVARCHAR(100) NOT NULL, -- 'First Floor', 'Second Floor', etc.
        [ImagePath] NVARCHAR(1000) NOT NULL,
        [Bedrooms] INT NULL,
        [Bathrooms] INT NULL,
        [Area] DECIMAL(18,2) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [SortOrder] INT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_PropertyFloorPlan] PRIMARY KEY CLUSTERED ([FloorPlanId] ASC),
        CONSTRAINT [FK_PropertyFloorPlan_Property] FOREIGN KEY ([PropertyId]) 
            REFERENCES [dbo].[Properties] ([PropertyId]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_PropertyFloorPlan_PropertyId] ON [dbo].[PropertyFloorPlan]([PropertyId]);
END
GO

-- 3. Bảng PropertyAttachment - Lưu file đính kèm (PDF, DOC, etc.)
-- ĐÃ BỎ - Không sử dụng
-- IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PropertyAttachment]') AND type in (N'U'))
-- BEGIN
--     CREATE TABLE [dbo].[PropertyAttachment] (
--         [AttachmentId] INT IDENTITY(1,1) NOT NULL,
--         [PropertyId] INT NOT NULL,
--         [FileName] NVARCHAR(255) NOT NULL,
--         [FilePath] NVARCHAR(1000) NOT NULL,
--         [FileType] NVARCHAR(50) NULL, -- 'pdf', 'doc', 'docx', 'xls', etc.
--         [FileSize] BIGINT NULL, -- Size in bytes
--         [IconPath] NVARCHAR(1000) NULL, -- Icon để hiển thị
--         [Description] NVARCHAR(MAX) NULL,
--         [SortOrder] INT NULL DEFAULT 0,
--         [CreatedAt] DATETIME2 NULL DEFAULT GETUTCDATE(),
--         CONSTRAINT [PK_PropertyAttachment] PRIMARY KEY CLUSTERED ([AttachmentId] ASC),
--         CONSTRAINT [FK_PropertyAttachment_Property] FOREIGN KEY ([PropertyId]) 
--             REFERENCES [dbo].[Properties] ([PropertyId]) ON DELETE CASCADE
--     );
--     
--     CREATE INDEX [IX_PropertyAttachment_PropertyId] ON [dbo].[PropertyAttachment]([PropertyId]);
-- END
-- GO

-- 4. Bảng Property360View - Lưu ảnh 360 độ
-- ĐÃ BỎ - Không sử dụng
-- IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Property360View]') AND type in (N'U'))
-- BEGIN
--     CREATE TABLE [dbo].[Property360View] (
--         [View360Id] INT IDENTITY(1,1) NOT NULL,
--         [PropertyId] INT NOT NULL,
--         [ImagePath] NVARCHAR(1000) NOT NULL,
--         [Title] NVARCHAR(255) NULL,
--         [Description] NVARCHAR(MAX) NULL,
--         [ViewUrl] NVARCHAR(1000) NULL, -- URL cho 360 viewer nếu có
--         [SortOrder] INT NULL DEFAULT 0,
--         [IsPrimary] BIT NULL DEFAULT 0,
--         [CreatedAt] DATETIME2 NULL DEFAULT GETUTCDATE(),
--         CONSTRAINT [PK_Property360View] PRIMARY KEY CLUSTERED ([View360Id] ASC),
--         CONSTRAINT [FK_Property360View_Property] FOREIGN KEY ([PropertyId]) 
--             REFERENCES [dbo].[Properties] ([PropertyId]) ON DELETE CASCADE
--     );
--     
--     CREATE INDEX [IX_Property360View_PropertyId] ON [dbo].[Property360View]([PropertyId]);
-- END
-- GO

PRINT 'All tables created successfully!';
GO

