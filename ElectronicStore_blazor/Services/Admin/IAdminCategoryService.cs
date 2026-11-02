using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services.Admin;

/// <summary>
/// Service quản lý danh mục ngành hàng dành riêng cho phân hệ Quản trị (Admin)
/// </summary>
public interface IAdminCategoryService
{
    List<Category> GetCategories(string? search = null);
    Category? GetCategoryById(int id);
    void SaveCategory(Category category);
    bool ToggleCategoryActive(int id);
    (bool Success, string Message) DeleteCategory(int id);
}
