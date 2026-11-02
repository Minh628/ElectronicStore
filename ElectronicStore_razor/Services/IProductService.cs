using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

public class ProductCatalogResult
{
    public List<Product> Products { get; set; } = new();
    public int TotalProducts { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

/// <summary>
/// Service quản lý sản phẩm dành riêng cho End-User (Khách mua hàng)
/// </summary>
public interface IProductService
{
    Task<ProductCatalogResult> GetCatalogProductsAsync(int? categoryId, string? brand, string? search, string? sortBy, int currentPage = 1, int pageSize = 8);
    Task<List<string>> GetActiveBrandsAsync();
    Task<Product?> GetProductDetailAsync(int? id);
    Task<List<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int count = 4);
    Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids);
    Task<Product?> GetProductForCartAsync(int productId);
}
