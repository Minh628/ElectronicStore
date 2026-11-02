using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Areas.Admin.Services;

/// <summary>
/// Service chuyên trách quản trị ngành hàng/danh mục dành riêng cho phân hệ Admin
/// </summary>
public interface IAdminCategoryService
{
    Task<List<Category>> GetAllCategoriesAsync(string? search = null);
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<(bool Success, string Message)> SaveCategoryAsync(Category category);
    Task<(bool Success, string Message)> ToggleCategoryStatusAsync(int id);
    Task<(bool Success, string Message)> DeleteCategoryAsync(int id);
}

