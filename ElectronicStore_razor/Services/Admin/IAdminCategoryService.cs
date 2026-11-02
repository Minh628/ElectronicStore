using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services.Admin;

/// <summary>
/// Service quản lý danh mục sản phẩm dành riêng cho phân hệ Quản trị (Admin)
/// </summary>
public interface IAdminCategoryService
{
    Task<List<Category>> GetAllCategoriesAsync(string? search = null);
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<(bool Success, string Message)> SaveCategoryAsync(Category category);
    Task<(bool Success, string Message)> ToggleCategoryStatusAsync(int id);
    Task<(bool Success, string Message)> DeleteCategoryAsync(int id);
}
