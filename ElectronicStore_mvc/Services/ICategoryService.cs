using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

/// <summary>
/// Service quản lý danh mục sản phẩm (Category Domain)
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Lấy danh sách danh mục đang hoạt động (dành cho Header, Sub-Nav và bộ lọc khách hàng)
    /// </summary>
    Task<List<Category>> GetActiveCategoriesAsync();

    /// <summary>
    /// Lấy danh sách danh mục đang hoạt động (đồng bộ, tiện dụng cho Razor View)
    /// </summary>
    List<Category> GetActiveCategories();

    /// <summary>
    /// Lấy toàn bộ danh mục kèm số lượng sản phẩm (dành cho trang Admin)
    /// </summary>
    Task<List<Category>> GetAllCategoriesAsync(string? search = null);

    /// <summary>
    /// Lấy chi tiết danh mục theo ID
    /// </summary>
    Task<Category?> GetCategoryByIdAsync(int id);

    /// <summary>
    /// Thêm mới hoặc cập nhật thông tin danh mục
    /// </summary>
    Task<(bool Success, string Message)> SaveCategoryAsync(Category category);

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động của danh mục (khi ẩn danh mục thì toàn bộ sản phẩm liên quan cũng bị ẩn)
    /// </summary>
    Task<(bool Success, string Message)> ToggleCategoryStatusAsync(int id);

    /// <summary>
    /// Xóa danh mục: Tự động chuyển sang Tạm ẩn (Soft Delete) nếu có sản phẩm, hoặc Xóa vĩnh viễn nếu trống
    /// </summary>
    Task<(bool Success, string Message)> DeleteCategoryAsync(int id);
}

