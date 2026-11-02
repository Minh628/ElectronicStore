using Microsoft.EntityFrameworkCore;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

public class CategoryService : ICategoryService
{
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;

    public CategoryService(IDbContextFactory<ElectronicStoreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public List<Category> GetActiveCategories()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Categories.Where(c => c.IsActive).OrderBy(c => c.Id).ToList();
    }

    public Category? GetCategoryById(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Categories.FirstOrDefault(c => c.Id == id && c.IsActive);
    }
}
