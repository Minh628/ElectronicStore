using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Service quản lý thiết bị/sản phẩm dành riêng cho End-User (Khách mua hàng)
/// </summary>
public interface IProductService
{
    List<Product> GetAllProducts(int? categoryId = null, string? search = null);
    Product? GetProductById(int id);
    List<string> GetBrands();
    List<Product> GetRelatedProducts(int categoryId, int excludeProductId, int count = 4);
    bool BuyProduct(int id);
}
