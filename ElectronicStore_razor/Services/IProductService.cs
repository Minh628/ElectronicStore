using ElectronicStore_razor.Models;

namespace ElectronicStore_razor.Services;

public interface IProductService
{
    List<Product> GetAllProducts();
    Product? GetProductById(int id);
    bool BuyProduct(int id);
    
    // Các phương thức phục vụ quản trị viên (Admin)
    void AddProduct(Product product);
    bool UpdateProduct(Product product);
    bool DeleteProduct(int id);
}
