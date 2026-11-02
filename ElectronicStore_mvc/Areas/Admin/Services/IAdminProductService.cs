using Microsoft.AspNetCore.Http;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Areas.Admin.Services;

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
/// Service chuyên trách quản trị sản phẩm/thiết bị điện tử dành riêng cho phân hệ Admin
/// </summary>
public interface IAdminProductService
{
    Task<ProductAdminResult> GetAdminProductsAsync(int? categoryId, string? search, int currentPage = 1, int pageSize = 10);
    Task<(bool Success, string Message)> SaveProductAsync(Product product, IFormFile? imageFile, string webRootPath);
    Task<(bool Success, string Message)> DeleteProductAsync(int id);
    Task<(bool Success, string Message)> ToggleProductStatusAsync(int id);
}

