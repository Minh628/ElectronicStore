using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Service quản lý danh mục sản phẩm dành riêng cho End-User (Khách mua hàng)
/// </summary>
public interface ICategoryService
{
    List<Category> GetActiveCategories();
    Category? GetCategoryById(int id);
}
