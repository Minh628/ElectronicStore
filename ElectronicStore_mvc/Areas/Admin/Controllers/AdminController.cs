using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;

using ElectronicStore_mvc.Areas.Admin.Models;

namespace ElectronicStore_mvc.Areas.Admin.Controllers;



[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ElectronicStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        ElectronicStoreDbContext context, 
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // Mặc định chuyển hướng vào trang quản lý sản phẩm
    public IActionResult Index() => RedirectToAction(nameof(Products));

    #region 1. QUẢN LÝ SẢN PHẨM (PRODUCTS)
    public async Task<IActionResult> Products(int? categoryId, string? search, int currentPage = 1)
    {
        var allQuery = _context.Products.AsQueryable();
        ViewBag.TotalSystemProducts = await allQuery.CountAsync();
        ViewBag.InStockCount = await allQuery.CountAsync(p => p.Quantity > 0);
        ViewBag.LowOrOutCount = await allQuery.CountAsync(p => p.Quantity <= 5);

        var query = _context.Products.Include(p => p.Category).AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Brand.Contains(search));
        }

        const int pageSize = 10;
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages) currentPage = totalPages;

        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.Search = search;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = totalPages;
        ViewBag.CurrentPage = currentPage;

        var products = await query.OrderByDescending(p => p.Id)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return View(products);
    }

    [HttpPost]
    public async Task<IActionResult> SaveProduct(Product product, IFormFile? imageFile, int? filterCategoryId, int? categoryId, string? search, int currentPage = 1)
    {
        if (string.IsNullOrWhiteSpace(product.Name) || product.Price <= 0 || product.Quantity < 0 || product.CategoryId <= 0)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Tên, Danh mục, Đơn giá (> 0) và Tồn kho hợp lệ!";
            return RedirectToAction(nameof(Products), new { categoryId = filterCategoryId, search, currentPage });
        }
        // Xử lý upload ảnh từ máy tính (Desktop) nếu người dùng chọn file
        if (imageFile != null && imageFile.Length > 0)
        {
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Ảnh sản phẩm không được vượt quá 5 MB.";
                return RedirectToAction(nameof(Products), new { categoryId = filterCategoryId, search, currentPage });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.";
                return RedirectToAction(nameof(Products), new { categoryId = filterCategoryId, search, currentPage });
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
            product.ImageUrl = $"/images/products/{uniqueFileName}";
        }

        if (product.Id == 0)
        {
            _context.Products.Add(product);
            TempData["SuccessMessage"] = $"Đã thêm mới thiết bị '{product.Name}' thành công!";
        }
        else
        {
            var existing = await _context.Products.FindAsync(product.Id);
            if (existing != null)
            {
                existing.Name = product.Name;
                existing.Brand = product.Brand;
                existing.CategoryId = product.CategoryId;
                existing.Price = product.Price;
                existing.Quantity = product.Quantity;
                existing.WarrantyMonths = product.WarrantyMonths;
                if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                {
                    existing.ImageUrl = product.ImageUrl;
                }
                existing.Specifications = product.Specifications;
                // Bổ sung cập nhật mô tả chi tiết bài viết tính năng sản phẩm
                existing.Description = product.Description;
                existing.IsActive = product.IsActive;
                TempData["SuccessMessage"] = $"Đã cập nhật thành công thiết bị '{existing.Name}'!";
            }
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Products), new { categoryId = filterCategoryId, search, currentPage });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProduct(int id, int? categoryId, int? filterCategoryId, string? search, int currentPage = 1)
    {
        var prod = await _context.Products.FindAsync(id);
        if (prod != null)
        {
            // Kiểm tra xem sản phẩm đã từng phát sinh hóa đơn bán hàng hay chưa
            bool hasOrders = await _context.OrderDetails.AnyAsync(d => d.ProductId == id);
            if (hasOrders)
            {
                // Nếu đã có đơn hàng -> Soft Delete (Ngừng kinh doanh) để bảo toàn lịch sử hóa đơn kế toán
                prod.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["WarningMessage"] = $"Thiết bị '{prod.Name}' đã có lịch sử đơn hàng. Hệ thống đã tự động chuyển sang trạng thái TẠM ẨN / NGỪNG KINH DOANH.";
            }
            else
            {
                // Nếu chưa có đơn hàng -> Xóa vĩnh viễn (Hard Delete)
                _context.Products.Remove(prod);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn thiết bị '{prod.Name}'.";
            }
        }
        return RedirectToAction(nameof(Products), new { categoryId = filterCategoryId, search, currentPage });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleProductStatus(int id, int? categoryId, string? search, int currentPage = 1)
    {
        var prod = await _context.Products.FindAsync(id);
        if (prod != null)
        {
            prod.IsActive = !prod.IsActive;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã {(prod.IsActive ? "bật hiển thị" : "tạm ẩn")} thiết bị '{prod.Name}'!";
        }
        return RedirectToAction(nameof(Products), new { categoryId, search, currentPage });
    }
    #endregion

    #region 2. QUẢN LÝ DANH MỤC (CATEGORIES)
    public async Task<IActionResult> Categories(string? search)
    {
        var query = _context.Categories.Include(c => c.Products).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(kw) || 
                                     (c.Description != null && c.Description.ToLower().Contains(kw)));
        }
        ViewBag.SearchKeyword = search;
        var categories = await query.OrderBy(c => c.Id).ToListAsync();
        return View(categories);
    }

    [HttpPost]
    public async Task<IActionResult> SaveCategory(Category category, string? search)
    {
        // Kiểm tra tính hợp lệ: Tên ngành hàng không được để trống
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            TempData["ErrorMessage"] = "Tên ngành hàng không được để trống!";
            return RedirectToAction(nameof(Categories), new { search });
        }

        if (category.Id == 0)
        {
            category.Name = category.Name.Trim();
            category.Description = category.Description?.Trim();
            _context.Categories.Add(category);
            TempData["SuccessMessage"] = $"Đã thêm danh mục mới: {category.Name}";
        }
        else
        {
            var existing = await _context.Categories.FindAsync(category.Id);
            if (existing != null)
            {
                existing.Name = category.Name.Trim();
                existing.Description = category.Description?.Trim();
                existing.IsActive = category.IsActive;
                TempData["SuccessMessage"] = $"Đã cập nhật danh mục: {existing.Name}";
            }
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Categories), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleCategoryStatus(int id, string? search)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category != null)
        {
            // Đảo trạng thái hiển thị của danh mục; khi ẩn danh mục thì toàn bộ sản phẩm thuộc danh mục đó cũng tự động ẩn trên trang End-User
            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();
            var prodCount = category.Products?.Count ?? 0;
            TempData["SuccessMessage"] = category.IsActive
                ? $"Đã hiển thị lại danh mục '{category.Name}' và các sản phẩm thuộc danh mục này trên trang bán hàng!"
                : $"Đã tạm ẩn danh mục '{category.Name}' cùng toàn bộ {prodCount} sản phẩm thuộc danh mục này khỏi trang khách hàng!";
        }
        return RedirectToAction(nameof(Categories), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, string? search)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category != null)
        {
            if (category.Products.Count > 0)
            {
                // Nếu danh mục đang chứa sản phẩm -> Tự động chuyển sang Tạm ẩn (Soft Delete) để ẩn toàn bộ sản phẩm liên kết trên trang End-User
                category.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Danh mục '{category.Name}' đang có {category.Products.Count} sản phẩm liên kết nên đã được chuyển sang trạng thái TẠM ẨN (toàn bộ sản phẩm thuộc danh mục này cũng được ẩn khỏi trang khách hàng).";
            }
            else
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn danh mục '{category.Name}'.";
            }
        }
        return RedirectToAction(nameof(Categories), new { search });
    }
    #endregion

    public static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return "Pending";
        return status switch
        {
            "Chờ xử lý" or "Pending" => "Pending",
            "Đã duyệt" or "Confirmed" => "Confirmed",
            "Đang giao hàng" or "Đang giao" or "Shipping" => "Shipping",
            "Hoàn thành" or "Completed" or "Đã giao" or "Hoàn tất" => "Completed",
            "Đã hủy" or "Cancelled" => "Cancelled",
            _ => status
        };
    }

    #region 3. QUẢN LÝ ĐƠN HÀNG (ORDERS)
    public async Task<IActionResult> Orders(string? status, string? search)
    {
        var allOrders = await _context.Orders.ToListAsync();
        ViewBag.TotalOrdersCount = allOrders.Count;
        ViewBag.PendingOrdersCount = allOrders.Count(o => NormalizeStatus(o.OrderStatus) == "Pending");
        ViewBag.ShippingOrdersCount = allOrders.Count(o => NormalizeStatus(o.OrderStatus) == "Shipping");
        ViewBag.CompletedOrdersCount = allOrders.Count(o => NormalizeStatus(o.OrderStatus) == "Completed");
        // Thống kê tổng doanh thu thực tế từ các đơn hoàn thành
        ViewBag.TotalRevenue = allOrders.Where(o => NormalizeStatus(o.OrderStatus) == "Completed").Sum(o => o.TotalAmount);

        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            switch (status)
            {
                case "Pending":
                    query = query.Where(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ xử lý");
                    break;
                case "Confirmed":
                    query = query.Where(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Đã duyệt");
                    break;
                case "Shipping":
                    query = query.Where(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao" || o.OrderStatus == "Đang giao hàng");
                    break;
                case "Completed":
                    query = query.Where(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn thành" || o.OrderStatus == "Đã giao" || o.OrderStatus == "Hoàn tất");
                    break;
                case "Cancelled":
                    query = query.Where(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy");
                    break;
                default:
                    query = query.Where(o => o.OrderStatus == status);
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            var cleanKw = kw.Replace("#", "").Replace("es", "").TrimStart('0');
            int.TryParse(cleanKw, out int searchOrderId);

            query = query.Where(o => (searchOrderId > 0 && o.Id == searchOrderId) ||
                                     (o.ReceiverName != null && o.ReceiverName.ToLower().Contains(kw)) ||
                                     (o.ReceiverPhone != null && o.ReceiverPhone.Contains(kw)) ||
                                     (o.ShippingAddress != null && o.ShippingAddress.ToLower().Contains(kw)) ||
                                     (o.User != null && o.User.FullName != null && o.User.FullName.ToLower().Contains(kw)) ||
                                     (o.User != null && o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(kw)));
        }

        ViewBag.SelectedStatus = status;
        ViewBag.SearchKeyword = search;
        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, string status, string? currentStatus, string? search)
    {
        status = NormalizeStatus(status);
        if (status is not ("Pending" or "Confirmed" or "Shipping" or "Completed" or "Cancelled"))
        {
            TempData["ErrorMessage"] = "Trạng thái đơn hàng không hợp lệ.";
            return RedirectToAction(nameof(Orders), new { status = currentStatus, search });
        }

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order != null)
        {
            // 1. Nếu chuyển sang Cancelled mà trước đó chưa phải Cancelled -> hoàn lại tồn kho
            var previousStatus = NormalizeStatus(order.OrderStatus);
            if (status == "Cancelled" && previousStatus != "Cancelled")
            {
                foreach (var d in order.OrderDetails)
                {
                    var prod = await _context.Products.FindAsync(d.ProductId);
                    if (prod != null)
                    {
                        prod.Quantity += d.Quantity;
                    }
                }
            }
            // 2. Nếu từ Cancelled đổi lại trạng thái khác -> trừ lại kho
            else if (previousStatus == "Cancelled" && status != "Cancelled")
            {
                foreach (var d in order.OrderDetails)
                {
                    var prod = await _context.Products.FindAsync(d.ProductId);
                    if (prod != null)
                    {
                        prod.Quantity = Math.Max(0, prod.Quantity - d.Quantity);
                    }
                }
            }

            order.OrderStatus = status;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #ES{order.Id:D7} sang '{status}'.";
        }
        return RedirectToAction(nameof(Orders), new { status = currentStatus, search });
    }
    #endregion

    #region 4. QUẢN LÝ TÀI KHOẢN (ACCOUNTS)
    public async Task<IActionResult> Accounts(string? search)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(u => (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                                     (u.FullName != null && u.FullName.ToLower().Contains(kw)) ||
                                     (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                                     (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
        }

        ViewBag.SearchKeyword = search;
        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var list = new List<UserAccountViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            list.Add(new UserAccountViewModel
            {
                User = u,
                Role = roles.FirstOrDefault() ?? "Customer"
            });
        }
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(string username, string fullName, string email, string phone, string address, string password, string role, string? search)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] = "Tên đăng nhập và mật khẩu không được để trống!";
            return RedirectToAction(nameof(Accounts), new { search });
        }

        var existing = await _userManager.FindByNameAsync(username.Trim());
        if (existing != null)
        {
            TempData["ErrorMessage"] = $"Tên đăng nhập '{username}' đã được sử dụng!";
            return RedirectToAction(nameof(Accounts), new { search });
        }

        var user = new ApplicationUser
        {
            UserName = username.Trim(),
            FullName = string.IsNullOrWhiteSpace(fullName) ? username.Trim() : fullName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? $"{username.Trim()}@electrostore.vn" : email.Trim(),
            PhoneNumber = phone?.Trim(),
            Address = address?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Lỗi khi tạo tài khoản: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Accounts), new { search });
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }
        await _userManager.AddToRoleAsync(user, role);

        TempData["SuccessMessage"] = $"Đã tạo thành công tài khoản '{user.UserName}' với vai trò '{role}'!";
        return RedirectToAction(nameof(Accounts), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAccount(string userId, string fullName, string email, string phone, string address, string role, string? search)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng cần sửa!";
            return RedirectToAction(nameof(Accounts), new { search });
        }

        // Chặn Admin tự tước quyền quản trị của chính mình
        if (User.Identity != null && User.Identity.Name == user.UserName && role != "Admin")
        {
            TempData["ErrorMessage"] = "Bạn không thể tự hạ quyền Quản trị viên của chính mình!";
            return RedirectToAction(nameof(Accounts), new { search });
        }

        user.FullName = fullName?.Trim();
        user.Email = email?.Trim();
        user.PhoneNumber = phone?.Trim();
        user.Address = address?.Trim();

        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(role))
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật thông tin tài khoản '{user.UserName}'!";
        return RedirectToAction(nameof(Accounts), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAccount(string userId, string? search)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return RedirectToAction(nameof(Accounts), new { search });

        if (User.Identity != null && User.Identity.Name == user.UserName)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản Admin của chính mình!";
            return RedirectToAction(nameof(Accounts), new { search });
        }

        bool hasOrders = await _context.Orders.AnyAsync(o => o.UserId == user.Id);
        if (hasOrders)
        {
            user.IsActive = false;
            await _userManager.UpdateAsync(user);
            TempData["ErrorMessage"] = $"Tài khoản '{user.UserName}' đã có lịch sử đơn hàng. Hệ thống đã tự động chuyển sang trạng thái KHÓA TÀI KHOẢN để bảo toàn dữ liệu kế toán & hóa đơn.";
        }
        else
        {
            var res = await _userManager.DeleteAsync(user);
            if (res.Succeeded)
            {
                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn tài khoản '{user.UserName}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa: " + string.Join(", ", res.Errors.Select(e => e.Description));
            }
        }

        return RedirectToAction(nameof(Accounts), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAccountLock(string userId, string? search)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            if (User.Identity != null && User.Identity.Name == user.UserName)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản Admin của chính mình!";
                return RedirectToAction(nameof(Accounts), new { search });
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = user.IsActive 
                ? $"Đã MỞ KHÓA tài khoản '{user.UserName}' thành công!" 
                : $"Đã KHÓA tài khoản '{user.UserName}'!";
        }
        return RedirectToAction(nameof(Accounts), new { search });
    }
    #endregion
}
