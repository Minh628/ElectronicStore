using Microsoft.EntityFrameworkCore;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Triển khai nghiệp vụ danh mục sản phẩm trong Blazor Server
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;

    public CategoryService(IDbContextFactory<ElectronicStoreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public List<Category> GetCategories()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Categories.Include(c => c.Products).OrderBy(c => c.Id).ToList();
    }

    public List<Category> GetActiveCategories()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Categories.Where(c => c.IsActive).OrderBy(c => c.Id).ToList();
    }

    public void SaveCategory(Category category)
    {
        using var context = _contextFactory.CreateDbContext();
        if (category.Id == 0)
        {
            category.Name = category.Name.Trim();
            category.Description = category.Description?.Trim();
            context.Categories.Add(category);
        }
        else
        {
            var existing = context.Categories.Find(category.Id);
            if (existing != null)
            {
                existing.Name = category.Name.Trim();
                existing.Description = category.Description?.Trim();
                existing.IsActive = category.IsActive;
            }
        }
        context.SaveChanges();
    }

    public bool ToggleCategoryActive(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var category = context.Categories.Find(id);
        if (category != null)
        {
            category.IsActive = !category.IsActive;
            context.SaveChanges();
            return true;
        }
        return false;
    }

    public (bool Success, string Message) DeleteCategory(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var category = context.Categories.Include(c => c.Products).FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            return (false, "Không tìm thấy danh mục!");
        }

        if (category.Products.Any())
        {
            category.IsActive = false;
            context.SaveChanges();
            return (true, $"Danh mục '{category.Name}' đang có {category.Products.Count} sản phẩm liên kết nên được tự động chuyển sang trạng thái TẠM ẨN (ẩn toàn bộ sản phẩm thuộc danh mục trên cửa hàng) thay vì xóa vĩnh viễn.");
        }

        context.Categories.Remove(category);
        context.SaveChanges();
        return (true, $"Đã xóa vĩnh viễn danh mục: {category.Name}");
    }
}

