using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

/// <summary>
/// Service chuyên trách danh mục phục vụ khách hàng (End-User)
/// </summary>
public interface ICategoryService
{
    Task<List<Category>> GetActiveCategoriesAsync();
    List<Category> GetActiveCategories();
    Task<Category?> GetActiveCategoryByIdAsync(int id);
}
