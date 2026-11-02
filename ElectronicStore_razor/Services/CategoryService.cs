using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

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

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
    }
}
