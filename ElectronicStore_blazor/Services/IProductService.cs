using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

public class ProductCatalogResult
{
    public List<Product> Products { get; set; } = new();
    public int TotalProducts { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

/// <summary>
/// Service quản lý thiết bị/sản phẩm dành riêng cho End-User (Khách mua hàng)
/// </summary>
public interface IProductService
{
    List<Product> GetAllProducts(int? categoryId = null, string? search = null);
    ProductCatalogResult GetCatalogProducts(int? categoryId, string? brand, string? search, string? sortBy, int currentPage = 1, int pageSize = 8);
    Product? GetProductById(int id);
    List<string> GetBrands();
    List<Product> GetRelatedProducts(int categoryId, int excludeProductId, int count = 4);
    bool BuyProduct(int id);
}
