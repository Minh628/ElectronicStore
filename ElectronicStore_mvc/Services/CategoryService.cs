using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

/// <summary>
/// Triển khai nghiệp vụ danh mục End-User
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ElectronicStoreDbContext _context;

    public CategoryService(ElectronicStoreDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetActiveCategoriesAsync()
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public List<Category> GetActiveCategories()
    {
        return _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToList();
    }

    public async Task<Category?> GetActiveCategoryByIdAsync(int id)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
    }
}
