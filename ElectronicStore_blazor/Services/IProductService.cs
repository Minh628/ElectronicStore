using ElectronicStore_blazor.Models;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

public interface IProductService
{
    // 1. Phục vụ trang khách hàng
    List<Product> GetAllProducts(int? categoryId = null, string? search = null);
    Product? GetProductById(int id);
    bool BuyProduct(int id);

    // 2. Phục vụ quản trị sản phẩm
    List<Product> GetAllAdminProducts();
    void AddProduct(Product product);
    bool UpdateProduct(Product product);
    bool DeleteProduct(int id);
    bool ToggleProductActive(int id);

    // 3. Phục vụ quản trị danh mục
    List<Category> GetCategories();
    void SaveCategory(Category category);
    (bool Success, string Message) DeleteCategory(int id);

    // 4. Phục vụ quản lý đơn hàng
    List<Order> GetOrders(string? status = null);
    Order? GetOrderById(int id, string? requesterUserId = null, bool requesterIsAdmin = false, int? guestOrderId = null);
    bool UpdateOrderStatus(int orderId, string status);
    (bool Success, string Message) CancelOrder(int orderId, bool isClient = true, string? requesterUserId = null, string? requesterPhone = null);
    (bool Success, string Message, int OrderId) PlaceOrder(string fullName, string phone, string email, string address, string paymentMethod, string? notes, List<CartItem> items, string? userId = null);

    // 5. Phục vụ quản lý tài khoản
    List<ApplicationUser> GetUsers();
    bool ToggleUserLock(string userId);
}
