using Microsoft.EntityFrameworkCore;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

public class ProductService : IProductService
{
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;

    public ProductService(IDbContextFactory<ElectronicStoreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public List<Product> GetAllProducts(int? categoryId = null, string? search = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive)
            .AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(p => p.Name.Contains(keyword) ||
                                     p.Brand.Contains(keyword) ||
                                     (p.Description != null && p.Description.Contains(keyword)));
        }

        return query.OrderByDescending(p => p.Id).ToList();
    }

    public ProductCatalogResult GetCatalogProducts(
        int? categoryId, string? brand, string? search, string? sortBy, int currentPage = 1, int pageSize = 8)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive)
            .AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            var bLower = brand.Trim().ToLower();
            query = query.Where(p => p.Brand.ToLower() == bLower);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                     p.Brand.ToLower().Contains(kw) ||
                                     (p.Specifications != null && p.Specifications.ToLower().Contains(kw)));
        }

        int totalProducts = query.Count();
        int totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        if (totalPages < 1) totalPages = 1;
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages) currentPage = totalPages;

        query = sortBy switch
        {
            "price-asc" or "price_asc" => query.OrderBy(p => p.Price),
            "price-desc" or "price_desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.Id)
        };

        var products = query
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new ProductCatalogResult
        {
            Products = products,
            TotalProducts = totalProducts,
            TotalPages = totalPages,
            CurrentPage = currentPage
        };
    }

    public Product? GetProductById(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Products
            .Include(p => p.Category)
            .FirstOrDefault(p => p.Id == id && p.IsActive && p.Category != null && p.Category.IsActive);
    }

    public List<string> GetBrands()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Products
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive && !string.IsNullOrEmpty(p.Brand))
            .Select(p => p.Brand)
            .Distinct()
            .OrderBy(b => b)
            .ToList();
    }

    public List<Product> GetRelatedProducts(int categoryId, int excludeProductId, int count = 4)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Products
            .Where(p => p.CategoryId == categoryId && p.Id != excludeProductId && p.IsActive && p.Category != null && p.Category.IsActive)
            .Take(count)
            .ToList();
    }

    public bool BuyProduct(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var product = context.Products.Find(id);
        if (product != null && product.Quantity > 0)
        {
            product.Quantity--;
            context.SaveChanges();
            return true;
        }
        return false;
    }
}
