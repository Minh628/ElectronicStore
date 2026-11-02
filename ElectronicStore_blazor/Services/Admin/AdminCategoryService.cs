using Microsoft.EntityFrameworkCore;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services.Admin;

public class AdminCategoryService : IAdminCategoryService
{
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;

    public AdminCategoryService(IDbContextFactory<ElectronicStoreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public List<Category> GetCategories(string? search = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Categories.Include(c => c.Products).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(kw) ||
                                     (c.Description != null && c.Description.ToLower().Contains(kw)));
        }

        return query.OrderBy(c => c.Id).ToList();
    }

    public Category? GetCategoryById(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Categories.Include(c => c.Products).FirstOrDefault(c => c.Id == id);
    }

    public void SaveCategory(Category category)
    {
        using var context = _contextFactory.CreateDbContext();
        if (category.Id == 0)
        {
            context.Categories.Add(category);
        }
        else
        {
            var existing = context.Categories.Find(category.Id);
            if (existing != null)
            {
                existing.Name = category.Name;
                existing.Description = category.Description;
                existing.IsActive = category.IsActive;
            }
        }
        context.SaveChanges();
    }

    public bool ToggleCategoryActive(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var cat = context.Categories.Find(id);
        if (cat == null) return false;

        cat.IsActive = !cat.IsActive;
        context.SaveChanges();
        return true;
    }

    public (bool Success, string Message) DeleteCategory(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var cat = context.Categories.Include(c => c.Products).FirstOrDefault(c => c.Id == id);
        if (cat == null)
        {
            return (false, "Không tìm thấy danh mục cần xóa.");
        }

        var hasOrders = context.OrderDetails.Any(od => od.Product.CategoryId == id);
        if (hasOrders)
        {
            cat.IsActive = false;
            context.SaveChanges();
            return (true, $"Danh mục '{cat.Name}' đã có sản phẩm phát sinh đơn hàng trong quá khứ nên hệ thống đã chuyển sang chế độ TẠM ẨN!");
        }

        if (cat.Products != null && cat.Products.Any())
        {
            context.Products.RemoveRange(cat.Products);
        }

        context.Categories.Remove(cat);
        context.SaveChanges();
        return (true, $"Đã xóa vĩnh viễn danh mục '{cat.Name}'.");
    }
}
