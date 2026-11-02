using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

/// <summary>
/// Service quản lý sản phẩm dành riêng cho End-User (Khách mua hàng)
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
            "price-asc" or "price_asc" => query.OrderBy(p => p.Price),
            "price-desc" or "price_desc" => query.OrderByDescending(p => p.Price),
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
            .Where(p => ids.Contains(p.Id) && p.IsActive && p.Category != null && p.Category.IsActive)
            .ToListAsync();
    }

    public async Task<Product?> GetProductForCartAsync(int productId)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && p.Category != null && p.Category.IsActive);
    }
}
