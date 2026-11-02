using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

/// <summary>
/// Triển khai nghiệp vụ sản phẩm điện tử
/// </summary>
public class ProductService : IProductService
{
    private readonly ElectronicStoreDbContext _context;

    public ProductService(ElectronicStoreDbContext context)
    {
        _context = context;
    }

    public async Task<ProductCatalogResult> GetCatalogProductsAsync(
        int? categoryId, string? brand, string? search, string? sortBy, int currentPage = 1, int pageSize = 8)
    {
        // Điều kiện bảo mật nghiệp vụ: Sản phẩm và Danh mục tương ứng đều phải đang hoạt động (IsActive == true)
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            var brandLower = brand.Trim().ToLower();
            query = query.Where(p => p.Brand.ToLower() == brandLower);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                     p.Brand.ToLower().Contains(kw) ||
                                     (p.Specifications != null && p.Specifications.ToLower().Contains(kw)));
        }

        int totalProducts = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;

        query = sortBy switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.Id)
        };

        var products = await query
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ProductCatalogResult
        {
            Products = products,
            TotalProducts = totalProducts,
            TotalPages = totalPages,
            CurrentPage = currentPage
        };
    }

    public async Task<List<string>> GetActiveBrandsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive)
            .Select(p => p.Brand)
            .Distinct()
            .ToListAsync();
    }

    public async Task<Product?> GetProductDetailAsync(int? id)
    {
        if (id.HasValue && id.Value > 0)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id.Value && p.IsActive && p.Category != null && p.Category.IsActive);
        }

        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.IsActive && p.Category != null && p.Category.IsActive);
    }

    public async Task<List<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count = 4)
    {
        return await _context.Products
            .Where(p => p.CategoryId == categoryId && p.Id != excludeProductId && p.IsActive && p.Category != null && p.Category.IsActive)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<Product?> GetProductForCartAsync(int productId)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<ProductAdminResult> GetAdminProductsAsync(
        int? categoryId, string? search, int currentPage = 1, int pageSize = 10)
    {
        var allQuery = _context.Products.AsQueryable();
        int totalSystemProducts = await allQuery.CountAsync();
        int inStockCount = await allQuery.CountAsync(p => p.Quantity > 0);
        int lowOrOutCount = await allQuery.CountAsync(p => p.Quantity <= 5);

        var query = _context.Products.Include(p => p.Category).AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw) || p.Brand.ToLower().Contains(kw));
        }

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        if (totalPages < 1) totalPages = 1;
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages) currentPage = totalPages;

        var products = await query.OrderByDescending(p => p.Id)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ProductAdminResult
        {
            Products = products,
            TotalSystemProducts = totalSystemProducts,
            InStockCount = inStockCount,
            LowOrOutCount = lowOrOutCount,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CurrentPage = currentPage
        };
    }

    public async Task<(bool Success, string Message)> SaveProductAsync(
        Product product, IFormFile? imageFile, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(product.Name) || product.Price <= 0 || product.Quantity < 0 || product.CategoryId <= 0)
        {
            return (false, "Vui lòng nhập đầy đủ Tên, Danh mục, Đơn giá (> 0) và Tồn kho hợp lệ!");
        }

        // Xử lý upload ảnh an toàn
        if (imageFile != null && imageFile.Length > 0)
        {
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                return (false, "Ảnh sản phẩm không được vượt quá 5 MB.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return (false, "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");
            }

            var uploadsFolder = Path.Combine(webRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

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
            await _context.SaveChangesAsync();
            return (true, $"Đã thêm mới thiết bị '{product.Name}' thành công!");
        }
        else
        {
            var existing = await _context.Products.FindAsync(product.Id);
            if (existing == null)
            {
                return (false, "Không tìm thấy thiết bị cần cập nhật!");
            }

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
            existing.Description = product.Description;
            existing.IsActive = product.IsActive;

            await _context.SaveChangesAsync();
            return (true, $"Đã cập nhật thành công thiết bị '{existing.Name}'!");
        }
    }

    public async Task<(bool Success, string Message)> DeleteProductAsync(int id)
    {
        var prod = await _context.Products.FindAsync(id);
        if (prod == null)
        {
            return (false, "Không tìm thấy thiết bị!");
        }

        // Kiểm tra xem sản phẩm đã có lịch sử hóa đơn bán lẻ hay chưa
        bool hasOrders = await _context.OrderDetails.AnyAsync(d => d.ProductId == id);
        if (hasOrders)
        {
            // Soft delete: Chuyển sang tạm ẩn/ngừng kinh doanh
            prod.IsActive = false;
            await _context.SaveChangesAsync();
            return (true, $"Thiết bị '{prod.Name}' đã có lịch sử đơn hàng. Hệ thống đã tự động chuyển sang trạng thái TẠM ẨN / NGỪNG KINH DOANH.");
        }
        else
        {
            // Hard delete: Xóa vĩnh viễn
            _context.Products.Remove(prod);
            await _context.SaveChangesAsync();
            return (true, $"Đã xóa vĩnh viễn thiết bị '{prod.Name}'.");
        }
    }

    public async Task<(bool Success, string Message)> ToggleProductStatusAsync(int id)
    {
        var prod = await _context.Products.FindAsync(id);
        if (prod == null)
        {
            return (false, "Không tìm thấy thiết bị!");
        }

        prod.IsActive = !prod.IsActive;
        await _context.SaveChangesAsync();
        return (true, $"Đã {(prod.IsActive ? "bật hiển thị" : "tạm ẩn")} thiết bị '{prod.Name}'!");
    }
}

