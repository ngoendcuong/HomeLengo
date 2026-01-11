-- Tạo bảng FAQs (Câu hỏi thường gặp)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FAQs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FAQs] (
        [FaqId] INT IDENTITY(1,1) NOT NULL,
        [Question] NVARCHAR(500) NOT NULL,
        [Answer] NVARCHAR(MAX) NOT NULL,
        [Category] NVARCHAR(100) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK__FAQs__F6C1B8E5] PRIMARY KEY CLUSTERED ([FaqId] ASC)
    );
    
    PRINT 'Bảng FAQs đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT 'Bảng FAQs đã tồn tại.';
END
GO

-- Thêm một số dữ liệu mẫu (tùy chọn)
IF NOT EXISTS (SELECT * FROM [dbo].[FAQs])
BEGIN
    INSERT INTO [dbo].[FAQs] ([Question], [Answer], [Category], [SortOrder], [IsActive])
    VALUES
        (N'Tại sao tôi nên sử dụng dịch vụ của bạn?', 
         N'Sau khi tài khoản của bạn được thiết lập và bạn đã làm quen với nền tảng, bạn đã sẵn sàng bắt đầu sử dụng các dịch vụ của chúng tôi. Cho dù đó là truy cập các tính năng cụ thể, thực hiện giao dịch hay sử dụng các công cụ của chúng tôi, bạn sẽ tìm thấy mọi thứ mình cần trong tầm tay.', 
         N'Tổng quan', 1, 1),
        (N'Tôi có thể bắt đầu sử dụng dịch vụ của bạn như thế nào?', 
         N'Sau khi tài khoản của bạn được thiết lập và bạn đã làm quen với nền tảng, bạn đã sẵn sàng bắt đầu sử dụng các dịch vụ của chúng tôi. Cho dù đó là truy cập các tính năng cụ thể, thực hiện giao dịch hay sử dụng các công cụ của chúng tôi, bạn sẽ tìm thấy mọi thứ mình cần trong tầm tay.', 
         N'Tổng quan', 2, 1),
        (N'Bạn tính phí như thế nào?', 
         N'Sau khi tài khoản của bạn được thiết lập và bạn đã làm quen với nền tảng, bạn đã sẵn sàng bắt đầu sử dụng các dịch vụ của chúng tôi. Cho dù đó là truy cập các tính năng cụ thể, thực hiện giao dịch hay sử dụng các công cụ của chúng tôi, bạn sẽ tìm thấy mọi thứ mình cần trong tầm tay.', 
         N'Chi phí và Thanh toán', 1, 1);
    
    PRINT 'Đã thêm dữ liệu mẫu vào bảng FAQs.';
END
GO


