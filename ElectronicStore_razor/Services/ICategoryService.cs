using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

/// <summary>
/// Service quản lý danh mục sản phẩm (Category Domain) cho Razor Pages
/// </summary>
public interface ICategoryService
{
    Task<List<Category>> GetActiveCategoriesAsync();
    List<Category> GetActiveCategories();
    Task<List<Category>> GetAllCategoriesAsync(string? search = null);
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<(bool Success, string Message)> SaveCategoryAsync(Category category);
    Task<(bool Success, string Message)> ToggleCategoryStatusAsync(int id);
    Task<(bool Success, string Message)> DeleteCategoryAsync(int id);
}

