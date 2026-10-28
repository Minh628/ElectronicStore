using Microsoft.AspNetCore.Identity;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Data;

public static class DbSeeder
{
    /// <summary>
    /// Tự động khởi tạo Roles và Tài khoản mẫu chuẩn Identity nếu chưa tồn tại
    /// </summary>
    public static async Task SeedIdentityAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Tạo 2 Roles chuẩn: Admin và Customer
            string[] roles = { "Admin", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Tạo hoặc cập nhật tài khoản Admin mặc định (admin / admin123)
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@electrostore.vn",
                    FullName = "Quản Trị Viên Hệ Thống",
                    PhoneNumber = "0900000001",
                    Address = "Trụ sở ElectroStore, Hà Nội",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                var createAdmin = await userManager.CreateAsync(adminUser, "admin123");
                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                // Tự động chuẩn hóa FullName tiếng Việt nếu database bị lỗi encoding từ SQL script
                if (string.IsNullOrEmpty(adminUser.FullName) || adminUser.FullName.Contains("Quá") || adminUser.FullName.Contains("Ã"))
                {
                    adminUser.FullName = "Quản Trị Viên Hệ Thống";
                    await userManager.UpdateAsync(adminUser);
                }

                // Đảm bảo mật khẩu 'admin123' chuẩn Identity
                if (!await userManager.CheckPasswordAsync(adminUser, "admin123"))
                {
                    adminUser.PasswordHash = userManager.PasswordHasher.HashPassword(adminUser, "admin123");
                    await userManager.UpdateAsync(adminUser);
                }
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Tạo hoặc cập nhật tài khoản Khách hàng mẫu (nguyenvana / 123456)
            var customerUser = await userManager.FindByNameAsync("nguyenvana");
            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = "nguyenvana",
                    Email = "vana@gmail.com",
                    FullName = "Nguyễn Văn A",
                    PhoneNumber = "0901234567",
                    Address = "123 Nguyễn Huệ, Quận 1, TP.HCM",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                var createCust = await userManager.CreateAsync(customerUser, "123456");
                if (createCust.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "Customer");
                }
            }
            else
            {
                // Đảm bảo mật khẩu '123456' chuẩn Identity
                if (!await userManager.CheckPasswordAsync(customerUser, "123456"))
                {
                    customerUser.PasswordHash = userManager.PasswordHasher.HashPassword(customerUser, "123456");
                    await userManager.UpdateAsync(customerUser);
                }
                if (!await userManager.IsInRoleAsync(customerUser, "Customer"))
                {
                    await userManager.AddToRoleAsync(customerUser, "Customer");
                }
            }

            // 4. Cập nhật tài khoản Khách hàng mẫu (tranthib / 123456)
            var customerUser2 = await userManager.FindByNameAsync("tranthib");
            if (customerUser2 != null)
            {
                if (!await userManager.CheckPasswordAsync(customerUser2, "123456"))
                {
                    customerUser2.PasswordHash = userManager.PasswordHasher.HashPassword(customerUser2, "123456");
                    await userManager.UpdateAsync(customerUser2);
                }
                if (!await userManager.IsInRoleAsync(customerUser2, "Customer"))
                {
                    await userManager.AddToRoleAsync(customerUser2, "Customer");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbSeeder] Lỗi khi seed Identity: {ex.Message}");
        }
    }

    /// <summary>
    /// Hàm tự động khắc phục lỗi font tiếng Việt (Mojibake/dấu hỏi) trong Database
    /// </summary>
    public static void FixVietnameseEncoding(ElectronicStoreDbContext context)
    {
        try
        {
            // 1. Cập nhật bảng Danh mục (Categories)
            var categories = context.Categories.ToList();
            foreach (var c in categories)
            {
                switch (c.Id)
                {
                    case 1:
                        c.Name = "Điện thoại thông minh";
                        c.Description = "Các dòng smartphone cao cấp và tầm trung";
                        break;
                    case 2:
                        c.Name = "Laptop & Máy tính xách tay";
                        c.Description = "Laptop văn phòng, đồ họa, gaming";
                        break;
                    case 3:
                        c.Name = "Máy tính bảng (Tablet)";
                        c.Description = "Tablet phục vụ học tập, giải trí và làm việc";
                        break;
                    case 4:
                        c.Name = "Tai nghe & Âm thanh";
                        c.Description = "Tai nghe chống ồn, loa bluetooth cao cấp";
                        break;
                    case 5:
                        c.Name = "Phụ kiện & Linh kiện";
                        c.Description = "Sạc nhanh, bàn phím cơ, chuột gaming";
                        break;
                }
            }

            // 2. Cập nhật bảng Sản phẩm (Products)
            var products = context.Products.ToList();
            foreach (var p in products)
            {
                switch (p.Id)
                {
                    case 1:
                        p.Name = "iPhone 15 Pro Max 256GB";
                        p.Brand = "Apple";
                        p.Specifications = "Chip A17 Pro, RAM 8GB, Màn hình 6.7 inch OLED 120Hz, Pin 4422mAh";
                        p.Description = "Flagship cao cấp nhất của Apple với khung viền Titan và camera zoom quang học 5x.";
                        break;
                    case 2:
                        p.Name = "Samsung Galaxy S24 Ultra 512GB";
                        p.Brand = "Samsung";
                        p.Specifications = "Chip Snapdragon 8 Gen 3, RAM 12GB, Bút S-Pen, Camera 200MP";
                        p.Description = "Điện thoại tích hợp Galaxy AI thông minh, hỗ trợ làm việc và nhiếp ảnh đỉnh cao.";
                        break;
                    case 3:
                        p.Name = "MacBook Pro 14 M3 Pro";
                        p.Brand = "Apple";
                        p.Specifications = "Chip Apple M3 Pro 11-core, RAM 18GB, SSD 512GB, Liquid Retina XDR 120Hz";
                        p.Description = "Hiệu năng đồ họa vượt trội, thời lượng pin lên đến 18 giờ liên tục.";
                        break;
                    case 4:
                        p.Name = "ASUS ROG Zephyrus G16 (2024)";
                        p.Brand = "ASUS";
                        p.Specifications = "Intel Core Ultra 9, RTX 4080 12GB, RAM 32GB, OLED 240Hz";
                        p.Description = "Laptop gaming mỏng nhẹ cao cấp hàng đầu với màn hình chuẩn điện ảnh OLED ROG Nebula.";
                        break;
                    case 5:
                        p.Name = "iPad Air M2 11 inch Wi-Fi 128GB";
                        p.Brand = "Apple";
                        p.Specifications = "Chip Apple M2, RAM 8GB, Màn hình Liquid Retina, Hỗ trợ Apple Pencil Pro";
                        p.Description = "Thiết kế siêu mỏng nhẹ, hiệu năng cực mạnh hỗ trợ học tập và thiết kế đồ họa.";
                        break;
                    case 6:
                        p.Name = "Tai nghe Sony WH-1000XM5";
                        p.Brand = "Sony";
                        p.Specifications = "Chống ồn chủ động ANC đỉnh cao, Driver 30mm, Pin 30 giờ, Bluetooth 5.2";
                        p.Description = "Tai nghe trùm đầu chống ồn số 1 thế giới với chất âm Hi-Res sắc nét.";
                        break;
                    case 7:
                        p.Name = "Apple AirPods Pro 2 (USB-C)";
                        p.Brand = "Apple";
                        p.Specifications = "Chip H2, Chống ồn gấp 2 lần, Chế độ thích ứng thông minh, Hộp sạc MagSafe";
                        p.Description = "Tai nghe in-ear không dây tiện dụng, đồng bộ hoàn hảo trong hệ sinh thái Apple.";
                        break;
                    case 8:
                        p.Name = "Bàn phím cơ không dây NuPhy Air75 V2";
                        p.Brand = "NuPhy";
                        p.Specifications = "Low Profile mechanical switch, kết nối 3 chế độ (2.4G/BT/Dây), RGB 16.8M màu";
                        p.Description = "Bàn phím cơ siêu mỏng thích hợp mang đi cà phê, làm việc với cả Mac và Windows.";
                        break;
                }
            }

            // 3. Cập nhật bảng Đơn hàng (Orders)
            var orders = context.Orders.ToList();
            foreach (var o in orders)
            {
                if (o.Id == 1)
                {
                    o.ReceiverName = "Nguyễn Văn A";
                    o.ReceiverPhone = "0901234567";
                    o.ShippingAddress = "123 Nguyễn Huệ, Quận 1, TP.HCM";
                    o.Notes = "Giao giờ hành chính";
                }
                else if (o.Id == 2)
                {
                    o.ReceiverName = "Nguyễn Văn A";
                    o.ReceiverPhone = "0901234567";
                    o.ShippingAddress = "456 Lê Lợi, Quận Hải Châu, Đà Nẵng";
                    o.Notes = "Gọi trước khi giao hàng";
                }

                // Chuẩn hóa OrderStatus sang chuẩn hệ thống
                if (o.OrderStatus == "Chờ xử lý") o.OrderStatus = "Pending";
                else if (o.OrderStatus == "Đang giao hàng" || o.OrderStatus == "Đang giao") o.OrderStatus = "Shipping";
                else if (o.OrderStatus == "Đã giao" || o.OrderStatus == "Hoàn tất") o.OrderStatus = "Completed";
                else if (o.OrderStatus == "Đã duyệt") o.OrderStatus = "Confirmed";
            }

            // 4. Cập nhật bảng Người dùng (AspNetUsers) sửa triệt để lỗi Mojibake
            var users = context.Users.ToList();
            foreach (var u in users)
            {
                if (u.UserName == "admin")
                {
                    u.FullName = "Quản Trị Viên Hệ Thống";
                    u.Address = "Trụ sở ElectroStore, Hà Nội";
                }
                else if (u.UserName == "nguyenvana")
                {
                    u.FullName = "Nguyễn Văn A";
                    u.Address = "123 Nguyễn Huệ, Quận 1, TP.HCM";
                }
                else if (u.UserName == "tranthib")
                {
                    u.FullName = "Trần Thị B";
                    u.Address = "456 Lê Lợi, Quận Hải Châu, Đà Nẵng";
                }
                else
                {
                    if (u.FullName != null && (u.FullName.Contains("Quá") || u.FullName.Contains("Ã") || u.FullName.Contains("»") || u.FullName.Contains("Ä")))
                    {
                        u.FullName = u.UserName;
                    }
                }
            }

            context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbSeeder] Lỗi khi sửa font tiếng Việt: {ex.Message}");
        }
    }
}
