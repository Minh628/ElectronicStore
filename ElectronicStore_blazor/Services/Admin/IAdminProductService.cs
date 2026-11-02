using Microsoft.AspNetCore.Components.Forms;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services.Admin;

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
/// Service quản lý thiết bị/sản phẩm dành riêng cho phân hệ Quản trị (Admin)
/// </summary>
public interface IAdminProductService
{
    ProductAdminResult GetAdminProducts(int? categoryId, string? search, int currentPage = 1, int pageSize = 10);
    Product? GetProductById(int id);
    void AddProduct(Product product);
    void UpdateProduct(Product product);
    (bool Success, bool SoftDeleted, string ProductName) DeleteProductWithStatus(int id);
    bool ToggleProductActive(int id);
    Task<string?> SaveProductImageAsync(IBrowserFile file, string webRootPath);
}
