using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Service quản lý danh mục sản phẩm (Category Domain) trong Blazor
/// </summary>
public interface ICategoryService
{
    List<Category> GetCategories();
    List<Category> GetActiveCategories();
    void SaveCategory(Category category);
    bool ToggleCategoryActive(int id);
    (bool Success, string Message) DeleteCategory(int id);
}

