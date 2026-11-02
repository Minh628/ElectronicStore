using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services.Admin;

public class AdminProductService : IAdminProductService
{
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;

    public AdminProductService(IDbContextFactory<ElectronicStoreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public ProductAdminResult GetAdminProducts(int? categoryId, string? search, int currentPage = 1, int pageSize = 10)
    {
        using var context = _contextFactory.CreateDbContext();

        var allProducts = context.Products.Include(p => p.Category).AsNoTracking().ToList();

        var result = new ProductAdminResult
        {
            TotalSystemProducts = allProducts.Count,
            InStockCount = allProducts.Count(p => p.Quantity > 0),
            LowOrOutCount = allProducts.Count(p => p.Quantity <= 5)
        };

        var query = allProducts.AsEnumerable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim();
            query = query.Where(p => p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                     p.Brand.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.OrderByDescending(p => p.Id).ToList();

        result.TotalCount = filtered.Count;
        result.TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);
        if (result.TotalPages < 1) result.TotalPages = 1;

        if (currentPage < 1) currentPage = 1;
        if (currentPage > result.TotalPages) currentPage = result.TotalPages;
        result.CurrentPage = currentPage;

        result.Products = filtered
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return result;
    }

    public Product? GetProductById(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
    }

    public void AddProduct(Product product)
    {
        using var context = _contextFactory.CreateDbContext();
        context.Products.Add(product);
        context.SaveChanges();
    }

    public void UpdateProduct(Product product)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.Products.Find(product.Id);
        if (existing != null)
        {
            existing.Name = product.Name;
            existing.Brand = product.Brand;
            existing.CategoryId = product.CategoryId;
            existing.Price = product.Price;
            existing.Quantity = product.Quantity;
            existing.WarrantyMonths = product.WarrantyMonths;
            existing.ImageUrl = product.ImageUrl;
            existing.Specifications = product.Specifications;
            existing.Description = product.Description;
            existing.IsActive = product.IsActive;
            context.SaveChanges();
        }
    }

    public (bool Success, bool SoftDeleted, string ProductName) DeleteProductWithStatus(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var prod = context.Products.Find(id);
        if (prod == null) return (false, false, string.Empty);

        string name = prod.Name;
        bool hasOrders = context.OrderDetails.Any(d => d.ProductId == id);

        if (hasOrders)
        {
            prod.IsActive = false;
            context.SaveChanges();
            return (true, true, name);
        }
        else
        {
            context.Products.Remove(prod);
            context.SaveChanges();
            return (true, false, name);
        }
    }

    public bool ToggleProductActive(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var prod = context.Products.Find(id);
        if (prod == null) return false;

        prod.IsActive = !prod.IsActive;
        context.SaveChanges();
        return true;
    }

    public async Task<string?> SaveProductImageAsync(IBrowserFile file, string webRootPath)
    {
        if (file == null) return null;

        var folderPath = Path.Combine(webRootPath, "images", "products");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileName = $"{Guid.NewGuid():N}_{file.Name}";
        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024))
        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await stream.CopyToAsync(fs);
        }

        return $"/images/products/{fileName}";
    }
}
