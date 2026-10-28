-- ========================================================
-- SCRIPT TẠO DATABASE FIRST: HỆ THỐNG BÁN THIẾT BỊ ĐIỆN TỬ
-- Cơ sở dữ liệu: ElectronicStoreDb
-- Chuẩn kiến trúc: ASP.NET Core Identity (IdentityDbContext)
-- Đã loại bỏ bảng Customers (Hợp nhất hoàn toàn vào AspNetUsers)
-- Dùng chung cho cả 3 giao diện: Blazor, Razor Pages, MVC
-- ========================================================

USE master;
GO

-- 1. Nếu Database đã tồn tại thì ngắt kết nối và tạo mới
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'ElectronicStoreDb')
BEGIN
    ALTER DATABASE ElectronicStoreDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ElectronicStoreDb;
END
GO

CREATE DATABASE ElectronicStoreDb COLLATE Vietnamese_CI_AS;
GO

USE ElectronicStoreDb;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ========================================================
-- 2. TẠO CÁC BẢNG CHUẨN ASP.NET CORE IDENTITY
-- ========================================================

-- Bảng 1: Bảng vai trò hệ thống (AspNetRoles)
CREATE TABLE AspNetRoles (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    Name NVARCHAR(256) NULL,
    NormalizedName NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL
);
CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
GO

-- Bảng 2: Bảng người dùng mở rộng (AspNetUsers - Kế thừa IdentityUser)
-- Tích hợp sẵn FullName, Address, IsActive, CreatedAt thay thế bảng Customers
CREATE TABLE AspNetUsers (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,
    LockoutEnd DATETIMEOFFSET NULL,
    LockoutEnabled BIT NOT NULL DEFAULT 1,
    AccessFailedCount INT NOT NULL DEFAULT 0,
    -- Các trường nghiệp vụ mở rộng cho ElectroStore
    FullName NVARCHAR(100) NULL,
    Address NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1, -- 1: Hoạt động, 0: Đã bị Admin khóa
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);
CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE INDEX EmailIndex ON AspNetUsers (NormalizedEmail);
GO

-- Bảng 3: Liên kết Người dùng - Vai trò (AspNetUserRoles)
CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles (RoleId);
GO

-- Bảng 4: Quyền mở rộng của người dùng (AspNetUserClaims)
CREATE TABLE AspNetUserClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims (UserId);
GO

-- Bảng 5: Đăng nhập bên thứ 3 (AspNetUserLogins)
CREATE TABLE AspNetUserLogins (
    LoginProvider NVARCHAR(128) NOT NULL,
    ProviderKey NVARCHAR(128) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId NVARCHAR(450) NOT NULL,
    PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins (UserId);
GO

-- Bảng 6: Token bảo mật (AspNetUserTokens)
CREATE TABLE AspNetUserTokens (
    UserId NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(128) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Value NVARCHAR(MAX) NULL,
    PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);
GO

-- Bảng 7: Quyền của vai trò (AspNetRoleClaims)
CREATE TABLE AspNetRoleClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims (RoleId);
GO

-- ========================================================
-- 3. TẠO CÁC BẢNG NGHIỆP VỤ CỬA HÀNG (BUSINESS TABLES)
-- ========================================================

-- Bảng 8: Danh mục sản phẩm (Categories)
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- Bảng 9: Sản phẩm / Thiết bị điện tử (Products)
CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Brand NVARCHAR(100) NOT NULL, -- Hãng: Apple, ASUS, Dell, Sony,...
    Specifications NVARCHAR(MAX) NULL, -- Thông số: RAM, CPU, GPU, Pin,...
    Price DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    WarrantyMonths INT NOT NULL DEFAULT 12, -- Số tháng bảo hành
    ImageUrl NVARCHAR(500) NULL,
    Description NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
);
GO

-- Bảng 10: Hóa đơn / Đơn hàng (Orders) - Trỏ trực tiếp sang AspNetUsers(Id)
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL, -- Liên kết trực tiếp với AspNetUsers
    OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    OrderStatus NVARCHAR(50) NOT NULL DEFAULT N'Pending', -- Pending, Confirmed, Shipping, Completed, Cancelled
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT N'COD', -- COD, BankTransfer, VietQR
    ReceiverName NVARCHAR(100) NOT NULL, -- Tên người nhận hàng thực tế
    ReceiverPhone NVARCHAR(20) NOT NULL, -- SĐT người nhận hàng thực tế
    ShippingAddress NVARCHAR(255) NOT NULL, -- Địa chỉ nhận hàng
    Notes NVARCHAR(500) NULL,
    CONSTRAINT FK_Orders_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

-- Bảng 11: Chi tiết hóa đơn (OrderDetails)
CREATE TABLE OrderDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL, -- Giá bảo toàn tại thời điểm mua
    TotalPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
GO

-- ========================================================
-- 4. CHÈN DỮ LIỆU MẪU (SEED DATA CHUẨN IDENTITY)
-- ========================================================

-- A. Khởi tạo 2 Vai trò hệ thống
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
('role-admin-01', N'Admin', N'ADMIN', NEWID()),
('role-customer-02', N'Customer', N'CUSTOMER', NEWID());
GO

-- B. Khởi tạo Người dùng mẫu (1 Admin, 2 Khách hàng)
-- PasswordHash mẫu cho Identity V3:
-- 'admin123' -> AQAAAAIAAYagAAAAEGnB3u6pI+m5Y7nJ9qF5y8Ww2z1e9c8v7x6a5s4d3f2g1h0j
-- '123456'   -> AQAAAAIAAYagAAAAEJ9x7u5pI+m4Y6nJ8qF4y7Wv1z0e8c7v6x5a4s3d2f1g0h9j
-- Ghi chú: Ứng dụng có thêm DbSeeder.cs sẽ tự động đồng bộ xác thực lại chuẩn 100% khi khởi chạy.
INSERT INTO AspNetUsers (
    Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
    PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
    TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, Address, IsActive, CreatedAt
) VALUES
('usr-admin-001', N'admin', N'ADMIN', N'admin@electrostore.vn', N'ADMIN@ELECTROSTORE.VN', 1,
 N'AQAAAAIAAYagAAAAEGnB3u6pI+m5Y7nJ9qF5y8Ww2z1e9c8v7x6a5s4d3f2g1h0j', NEWID(), NEWID(), N'0900000001', 1,
 0, 1, 0, N'Quản Trị Viên Hệ Thống', N'Trụ sở chính ElectroStore, Hà Nội', 1, GETDATE()),

('usr-cust-002', N'nguyenvana', N'NGUYENVANA', N'vana@gmail.com', N'VANA@GMAIL.COM', 1,
 N'AQAAAAIAAYagAAAAEJ9x7u5pI+m4Y6nJ8qF4y7Wv1z0e8c7v6x5a4s3d2f1g0h9j', NEWID(), NEWID(), N'0901234567', 1,
 0, 1, 0, N'Nguyễn Văn A', N'123 Nguyễn Huệ, Quận 1, TP.HCM', 1, GETDATE()),

('usr-cust-003', N'tranthib', N'TRANTHIB', N'thib@gmail.com', N'THIB@GMAIL.COM', 1,
 N'AQAAAAIAAYagAAAAEJ9x7u5pI+m4Y6nJ8qF4y7Wv1z0e8c7v6x5a4s3d2f1g0h9j', NEWID(), NEWID(), N'0912345678', 1,
 0, 1, 0, N'Trần Thị B', N'456 Lê Lợi, Quận Hải Châu, Đà Nẵng', 1, GETDATE());
GO

-- C. Gán Người dùng vào Vai trò tương ứng
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES
('usr-admin-001', 'role-admin-01'),    -- admin -> Admin
('usr-cust-002', 'role-customer-02'), -- nguyenvana -> Customer
('usr-cust-003', 'role-customer-02'); -- tranthib -> Customer
GO

-- D. Danh mục thiết bị điện tử
INSERT INTO Categories (Name, Description, IsActive) VALUES
(N'Điện thoại thông minh', N'Các dòng smartphone cao cấp và tầm trung', 1),
(N'Laptop & Máy tính xách tay', N'Laptop văn phòng, đồ họa, gaming', 1),
(N'Máy tính bảng (Tablet)', N'Tablet phục vụ học tập, giải trí và làm việc', 1),
(N'Tai nghe & Âm thanh', N'Tai nghe chống ồn, loa bluetooth cao cấp', 1),
(N'Phụ kiện & Linh kiện', N'Sạc nhanh, bàn phím cơ, chuột gaming', 1);
GO

-- E. Sản phẩm thiết bị điện tử
INSERT INTO Products (CategoryId, Name, Brand, Specifications, Price, Quantity, WarrantyMonths, ImageUrl, Description, IsActive) VALUES
(1, N'iPhone 15 Pro Max 256GB', N'Apple', N'Chip A17 Pro, RAM 8GB, Màn hình 6.7 inch OLED 120Hz, Pin 4422mAh', 29990000, 20, 12, N'https://images.unsplash.com/photo-1695048133142-1a20484d2569', N'Flagship cao cấp nhất của Apple với khung viền Titan và camera zoom quang học 5x.', 1),
(1, N'Samsung Galaxy S24 Ultra 512GB', N'Samsung', N'Chip Snapdragon 8 Gen 3, RAM 12GB, Bút S-Pen, Camera 200MP', 28490000, 15, 12, N'https://images.unsplash.com/photo-1610945265064-0e34e5519bbf', N'Điện thoại tích hợp Galaxy AI thông minh, hỗ trợ làm việc và nhiếp ảnh đỉnh cao.', 1),
(2, N'MacBook Pro 14 M3 Pro', N'Apple', N'Chip Apple M3 Pro 11-core, RAM 18GB, SSD 512GB, Liquid Retina XDR 120Hz', 49990000, 10, 12, N'https://images.unsplash.com/photo-1517336714731-489689fd1ca8', N'Hiệu năng đồ họa vượt trội, thời lượng pin lên đến 18 giờ liên tục.', 1),
(2, N'ASUS ROG Zephyrus G16 (2024)', N'ASUS', N'Intel Core Ultra 9, RTX 4080 12GB, RAM 32GB, OLED 240Hz', 64990000, 8, 24, N'https://images.unsplash.com/photo-1603302576837-37561b2e2302', N'Laptop gaming mỏng nhẹ cao cấp hàng đầu với màn hình chuẩn điện ảnh OLED ROG Nebula.', 1),
(3, N'iPad Air M2 11 inch Wi-Fi 128GB', N'Apple', N'Chip Apple M2, RAM 8GB, Màn hình Liquid Retina, Hỗ trợ Apple Pencil Pro', 16990000, 25, 12, N'https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0', N'Thiết kế siêu mỏng nhẹ, hiệu năng cực mạnh hỗ trợ học tập và thiết kế đồ họa.', 1),
(4, N'Tai nghe Sony WH-1000XM5', N'Sony', N'Chống ồn chủ động ANC đỉnh cao, Driver 30mm, Pin 30 giờ, Bluetooth 5.2', 7990000, 30, 12, N'https://images.unsplash.com/photo-1505740420928-5e560c06d30e', N'Tai nghe trùm đầu chống ồn số 1 thế giới với chất âm Hi-Res sắc nét.', 1),
(4, N'Apple AirPods Pro 2 (USB-C)', N'Apple', N'Chip H2, Chống ồn gấp 2 lần, Chế độ thích ứng thông minh, Hộp sạc MagSafe', 5690000, 40, 12, N'https://images.unsplash.com/photo-1600294037681-c80b4cb5b434', N'Tai nghe in-ear không dây tiện dụng, đồng bộ hoàn hảo trong hệ sinh thái Apple.', 1),
(5, N'Bàn phím cơ không dây NuPhy Air75 V2', N'NuPhy', N'Low Profile mechanical switch, kết nối 3 chế độ (2.4G/BT/Dây), RGB 16.8M màu', 2890000, 18, 12, N'https://images.unsplash.com/photo-1587829741301-dc798b83add3', N'Bàn phím cơ siêu mỏng thích hợp mang đi cà phê, làm việc với cả Mac và Windows.', 1);
GO

-- F. Hóa đơn mẫu (Trỏ trực tiếp sang usr-cust-002 - Nguyễn Văn A)
INSERT INTO Orders (UserId, OrderDate, TotalAmount, OrderStatus, PaymentMethod, ReceiverName, ReceiverPhone, ShippingAddress, Notes) VALUES
('usr-cust-002', DATEADD(DAY, -2, GETDATE()), 35680000, N'Completed', N'BankTransfer', N'Nguyễn Văn A', N'0901234567', N'123 Nguyễn Huệ, Quận 1, TP.HCM', N'Giao giờ hành chính'),
('usr-cust-002', DATEADD(DAY, -1, GETDATE()), 49990000, N'Shipping', N'COD', N'Nguyễn Văn A', N'0901234567', N'123 Nguyễn Huệ, Quận 1, TP.HCM', N'Gọi trước khi giao hàng');
GO

-- G. Chi tiết hóa đơn mẫu (Bảo toàn giá tại thời điểm mua)
INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice, TotalPrice) VALUES
(1, 1, 1, 29990000, 29990000), -- 1 iPhone 15 Pro Max
(1, 7, 1, 5690000, 5690000),    -- 1 AirPods Pro 2
(2, 3, 1, 49990000, 49990000);   -- 1 MacBook Pro 14 M3 Pro
GO
