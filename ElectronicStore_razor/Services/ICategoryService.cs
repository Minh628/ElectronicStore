using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

/// <summary>
/// Service quản lý danh mục sản phẩm dành riêng cho End-User (Khách mua hàng)
/// </summary>
public interface ICategoryService
{
    Task<List<Category>> GetActiveCategoriesAsync();
    List<Category> GetActiveCategories();
    Task<Category?> GetCategoryByIdAsync(int id);
}
