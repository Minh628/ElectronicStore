using Microsoft.AspNetCore.Http;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

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
/// Service quản lý thiết bị/sản phẩm điện tử (Product Domain) cho Razor Pages
/// </summary>
public interface IProductService
{
    // End-User
    Task<ProductCatalogResult> GetCatalogProductsAsync(int? categoryId, string? brand, string? search, string? sortBy, int currentPage = 1, int pageSize = 8);
    Task<List<string>> GetActiveBrandsAsync();
    Task<Product?> GetProductDetailAsync(int? id);
    Task<List<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count = 4);
    Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids);
    Task<Product?> GetProductForCartAsync(int productId);

    // Admin
    Task<ProductAdminResult> GetAdminProductsAsync(int? categoryId, string? brand, string? search, int currentPage = 1, int pageSize = 10);
    Task<(bool Success, string Message)> SaveProductAsync(Product product, IFormFile? imageFile, string webRootPath);
    Task<(bool Success, string Message)> DeleteProductAsync(int id);
    Task<(bool Success, string Message)> ToggleProductStatusAsync(int id);
}
