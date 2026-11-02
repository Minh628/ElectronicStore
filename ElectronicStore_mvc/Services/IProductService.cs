using Microsoft.AspNetCore.Http;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

public class ProductCatalogResult
{
    public List<Product> Products { get; set; } = new();
    public int TotalProducts { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

public class ProductAdminResult
{
    public List<Product> Products { get; set; } = new();
    public int TotalSystemProducts { get; set; }
    public int InStockCount { get; set; }
    public int LowOrOutCount { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

/// <summary>
/// Service quản lý thiết bị/sản phẩm điện tử (Product Domain)
/// </summary>
public interface IProductService
{
    // ================= DÀNH CHO KHÁCH HÀNG (END-USER) =================
    /// <summary>
    /// Lấy danh sách sản phẩm hiển thị trên trang chủ (lọc theo danh mục, thương hiệu, tìm kiếm, sắp xếp, phân trang)
    /// </summary>
    Task<ProductCatalogResult> GetCatalogProductsAsync(int? categoryId, string? brand, string? search, string? sortBy, int currentPage = 1, int pageSize = 8);

    /// <summary>
    /// Lấy danh sách thương hiệu của các sản phẩm đang hiển thị
    /// </summary>
    Task<List<string>> GetActiveBrandsAsync();

    /// <summary>
    /// Lấy thông tin chi tiết của sản phẩm (chỉ khi sản phẩm và danh mục liên kết đang hoạt động)
    /// </summary>
    Task<Product?> GetProductDetailAsync(int? id);

    /// <summary>
    /// Lấy danh sách sản phẩm gợi ý cùng danh mục
    /// </summary>
    Task<List<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count = 4);

    /// <summary>
    /// Lấy danh sách sản phẩm theo danh sách ID (dùng đồng bộ giỏ hàng)
    /// </summary>
    Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids);

    /// <summary>
    /// Lấy sản phẩm kiểm tra tồn kho cho giỏ hàng
    /// </summary>
    Task<Product?> GetProductForCartAsync(int productId);

    // ================= DÀNH CHO QUẢN TRỊ VIÊN (ADMIN) =================
    /// <summary>
    /// Lấy danh sách sản phẩm quản trị kèm các chỉ số thống kê tồn kho (KPIs)
    /// </summary>
    Task<ProductAdminResult> GetAdminProductsAsync(int? categoryId, string? search, int currentPage = 1, int pageSize = 10);

    /// <summary>
    /// Thêm mới hoặc cập nhật sản phẩm kèm xử lý upload ảnh
    /// </summary>
    Task<(bool Success, string Message)> SaveProductAsync(Product product, IFormFile? imageFile, string webRootPath);

    /// <summary>
    /// Xóa sản phẩm: Tự động chuyển sang Tạm ẩn (Soft Delete) nếu đã có đơn hàng, hoặc Xóa vĩnh viễn (Hard Delete)
    /// </summary>
    Task<(bool Success, string Message)> DeleteProductAsync(int id);

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động của sản phẩm
    /// </summary>
    Task<(bool Success, string Message)> ToggleProductStatusAsync(int id);
}

