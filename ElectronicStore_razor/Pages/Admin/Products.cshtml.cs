using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ProductsModel : PageModel
{
    private readonly ElectronicStoreDbContext _context;

    public ProductsModel(ElectronicStoreDbContext context)
    {
        _context = context;
    }

    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BrandFilter { get; set; }

    // Phân trang
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    // KPI thống kê toàn hệ thống
    public int TotalSystemProducts { get; set; }
    public int InStockCount { get; set; }
    public int LowOrOutCount { get; set; }

    // Model binding cho Form Thêm / Sửa
    [BindProperty]
    public Product InputProduct { get; set; } = new();

    // Hỗ trợ tải ảnh từ máy tính (Desktop)
    [BindProperty]
    public IFormFile? ImageUpload { get; set; }

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

        var allQuery = _context.Products.AsQueryable();
        TotalSystemProducts = await allQuery.CountAsync();
        InStockCount = await allQuery.CountAsync(p => p.Quantity > 0);
        LowOrOutCount = await allQuery.CountAsync(p => p.Quantity <= 5);

        var query = _context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw) || p.Brand.ToLower().Contains(kw));
        }

        if (CategoryId.HasValue && CategoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(BrandFilter))
        {
            query = query.Where(p => p.Brand == BrandFilter);
        }

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

        Products = await query
            .OrderByDescending(p => p.Id)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Thêm mới hoặc Cập nhật sản phẩm
    /// </summary>
    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(InputProduct.Name) || InputProduct.Price <= 0 || InputProduct.CategoryId <= 0)
        {
            AlertMessage = "Vui lòng nhập đầy đủ Tên, Danh mục và Giá sản phẩm hợp lệ!";
            AlertType = "danger";
            return RedirectToPage();
        }

        // Xử lý tải ảnh từ máy tính (Desktop) nếu người dùng chọn upload file
        if (ImageUpload != null && ImageUpload.Length > 0)
        {
            if (ImageUpload.Length > 5 * 1024 * 1024)
            {
                AlertMessage = "Ảnh sản phẩm không được vượt quá 5 MB.";
                AlertType = "danger";
                return RedirectToPage();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(ImageUpload.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                AlertMessage = "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.";
                AlertType = "danger";
                return RedirectToPage();
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ImageUpload.CopyToAsync(stream);
            }
            InputProduct.ImageUrl = $"/images/products/{uniqueFileName}";
        }

        if (InputProduct.Id == 0)
        {
            // Thêm mới
            InputProduct.IsActive = true;
            _context.Products.Add(InputProduct);
            AlertMessage = $"Đã thêm thành công sản phẩm: {InputProduct.Name}";
            AlertType = "success";
        }
        else
        {
            // Cập nhật
            var existing = await _context.Products.FindAsync(InputProduct.Id);
            if (existing != null)
            {
                existing.Name = InputProduct.Name.Trim();
                existing.Brand = InputProduct.Brand.Trim();
                existing.CategoryId = InputProduct.CategoryId;
                existing.Price = InputProduct.Price;
                existing.Quantity = InputProduct.Quantity;
                existing.WarrantyMonths = InputProduct.WarrantyMonths;
                if (!string.IsNullOrWhiteSpace(InputProduct.ImageUrl))
                {
                    existing.ImageUrl = InputProduct.ImageUrl;
                }
                existing.Specifications = InputProduct.Specifications;
                existing.Description = InputProduct.Description;
                existing.IsActive = InputProduct.IsActive;

                AlertMessage = $"Đã cập nhật thành công sản phẩm: {existing.Name}";
                AlertType = "success";
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    /// <summary>
    /// Xóa sản phẩm (An toàn: Tự động chuyển sang Soft Delete nếu sản phẩm đã phát sinh đơn hàng)
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            // Kiểm tra xem sản phẩm đã có trong hóa đơn nào chưa
            bool hasOrders = await _context.OrderDetails.AnyAsync(d => d.ProductId == id);
            if (hasOrders)
            {
                // Soft Delete: Ngừng kinh doanh để bảo toàn lịch sử kế toán đơn hàng
                product.IsActive = false;
                await _context.SaveChangesAsync();
                AlertMessage = $"Thiết bị '{product.Name}' đã có lịch sử đơn hàng. Hệ thống đã tự động chuyển sang trạng thái TẠM ẨN / NGỪNG KINH DOANH.";
                AlertType = "warning";
            }
            else
            {
                // Hard Delete nếu chưa từng phát sinh đơn hàng
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                AlertMessage = $"Đã xóa vĩnh viễn thiết bị '{product.Name}'.";
                AlertType = "success";
            }
        }
        return RedirectToPage();
    }

    /// <summary>
    /// Bật/Tắt trạng thái hiển thị kinh doanh của sản phẩm
    /// </summary>
    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            product.IsActive = !product.IsActive;
            await _context.SaveChangesAsync();
            AlertMessage = $"Đã đổi trạng thái sản phẩm: {product.Name}";
            AlertType = "info";
        }
        return RedirectToPage();
    }
}
