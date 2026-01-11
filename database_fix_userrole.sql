-- Script để sửa lỗi duplicate UserRole và đảm bảo mỗi user chỉ có 1 role
-- Chạy script này trong SQL Server Management Studio

-- Bước 1: Xóa các UserRole duplicate, chỉ giữ lại role mới nhất của mỗi user
WITH RankedUserRoles AS (
    SELECT 
        UserRoleId,
        UserId,
        RoleId,
        AssignedAt,
        ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY AssignedAt DESC) AS RowNum
    FROM UserRoles
)
DELETE FROM UserRoles
WHERE UserRoleId IN (
    SELECT UserRoleId 
    FROM RankedUserRoles 
    WHERE RowNum > 1
);

PRINT 'Đã xóa các UserRole duplicate';

-- Bước 2: Đảm bảo mỗi user có ít nhất 1 role (nếu không có thì tạo role mặc định = 3)
INSERT INTO UserRoles (UserId, RoleId, AssignedAt)
SELECT 
    u.UserId,
    3 AS RoleId, -- Role mặc định là User thường (role=3)
    GETUTCDATE() AS AssignedAt
FROM Users u
WHERE NOT EXISTS (
    SELECT 1 
    FROM UserRoles ur 
    WHERE ur.UserId = u.UserId
);

PRINT 'Đã tạo role mặc định cho các user chưa có role';

-- Bước 3: Cập nhật RoleId = 2 cho các user có gói dịch vụ đang active
UPDATE ur
SET ur.RoleId = 2, -- Agent
    ur.AssignedAt = GETUTCDATE()
FROM UserRoles ur
INNER JOIN UserServicePackages usp ON ur.UserId = usp.UserId
WHERE usp.IsActive = 1
    AND usp.EndDate IS NOT NULL
    AND usp.EndDate > GETDATE()
    AND ur.RoleId != 2;

PRINT 'Đã cập nhật RoleId = 2 cho các user có gói dịch vụ đang active';

-- Bước 4: Cập nhật RoleId = 3 cho các user có gói dịch vụ đã hết hạn
UPDATE ur
SET ur.RoleId = 3, -- User thường
    ur.AssignedAt = GETUTCDATE()
FROM UserRoles ur
INNER JOIN UserServicePackages usp ON ur.UserId = usp.UserId
WHERE usp.IsActive = 1
    AND usp.EndDate IS NOT NULL
    AND usp.EndDate <= GETDATE()
    AND ur.RoleId != 3;

PRINT 'Đã cập nhật RoleId = 3 cho các user có gói dịch vụ đã hết hạn';

-- Bước 5: Hiển thị kết quả
SELECT 
    u.UserId,
    u.Username,
    u.Email,
    ur.RoleId,
    r.RoleName,
    usp.IsActive AS HasActivePackage,
    usp.EndDate AS PackageEndDate,
    CASE 
        WHEN usp.EndDate IS NOT NULL AND usp.EndDate <= GETDATE() THEN 'Hết hạn'
        WHEN usp.IsActive = 1 THEN 'Đang active'
        ELSE 'Không có gói'
    END AS PackageStatus
FROM Users u
LEFT JOIN UserRoles ur ON u.UserId = ur.UserId
LEFT JOIN Roles r ON ur.RoleId = r.RoleId
LEFT JOIN UserServicePackages usp ON u.UserId = usp.UserId AND usp.IsActive = 1
ORDER BY u.UserId;

PRINT 'Hoàn tất sửa lỗi UserRole!';

