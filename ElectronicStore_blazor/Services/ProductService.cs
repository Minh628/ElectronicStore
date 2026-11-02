using Microsoft.EntityFrameworkCore;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Service quản lý thiết bị/sản phẩm điện tử (kết nối IDbContextFactory an toàn cho Blazor Server)
/// và điều phối các miền nghiệp vụ Category, Order thông qua các Service chuyên biệt
/// </summary>
public class ProductService : IProductService
{
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;
    private readonly ICategoryService _categoryService;
    private readonly IOrderService _orderService;

    public ProductService(
        IDbContextFactory<ElectronicStoreDbContext> contextFactory,
        ICategoryService categoryService,
        IOrderService orderService)
    {
        _contextFactory = contextFactory;
        _categoryService = categoryService;
        _orderService = orderService;
    }

    #region 1. SẢN PHẨM KHÁCH HÀNG (END-USER)
    public List<Product> GetAllProducts(int? categoryId = null, string? search = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Products.Include(p => p.Category)
            .Where(p => p.IsActive && p.Category != null && p.Category.IsActive).AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(p => p.Name.Contains(keyword) || p.Brand.Contains(keyword) || (p.Description != null && p.Description.Contains(keyword)));
        }

        return query.OrderByDescending(p => p.Id).ToList();
    }

    public Product? GetProductById(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Products.Include(p => p.Category)
            .FirstOrDefault(p => p.Id == id && p.IsActive && p.Category != null && p.Category.IsActive);
    }

    public bool BuyProduct(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var product = context.Products.Find(id);
        if (product != null && product.Quantity > 0)
        {
            product.Quantity--;
            context.SaveChanges();
            return true;
        }
        return false;
    }
    #endregion

    #region 2. QUẢN TRỊ SẢN PHẨM (ADMIN)
    public List<Product> GetAllAdminProducts()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Products
            .Include(p => p.Category)
            .OrderByDescending(p => p.Id)
            .ToList();
    }

    public void AddProduct(Product product)
    {
        using var context = _contextFactory.CreateDbContext();
        context.Products.Add(product);
        context.SaveChanges();
    }

    public bool UpdateProduct(Product product)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.Products.Find(product.Id);
        if (existing != null)
        {
            existing.Name = product.Name;
            existing.Brand = product.Brand;
            existing.CategoryId = product.CategoryId;
            existing.Price = product.Price;
            existing.Quantity = product.Quantity;
            existing.WarrantyMonths = product.WarrantyMonths;
            existing.ImageUrl = product.ImageUrl;
            existing.Specifications = product.Specifications;
            existing.Description = product.Description;
            existing.IsActive = product.IsActive;
            context.SaveChanges();
            return true;
        }
        return false;
    }

    public bool DeleteProduct(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var product = context.Products.Find(id);
        if (product != null)
        {
            bool hasOrders = context.OrderDetails.Any(d => d.ProductId == id);
            if (hasOrders)
            {
                product.IsActive = false;
                context.SaveChanges();
                return true;
            }

            context.Products.Remove(product);
            context.SaveChanges();
            return true;
        }
        return false;
    }

    public (bool Success, bool SoftDeleted, string ProductName) DeleteProductWithStatus(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var product = context.Products.Find(id);
        if (product == null)
        {
            return (false, false, string.Empty);
        }

        string prodName = product.Name;
        bool hasOrders = context.OrderDetails.Any(d => d.ProductId == id);
        if (hasOrders)
        {
            product.IsActive = false;
            context.SaveChanges();
            return (true, true, prodName);
        }

        context.Products.Remove(product);
        context.SaveChanges();
        return (true, false, prodName);
    }

    public bool ToggleProductActive(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var product = context.Products.Find(id);
        if (product != null)
        {
            product.IsActive = !product.IsActive;
            context.SaveChanges();
            return true;
        }
        return false;
    }
    #endregion

    #region 3. ĐIỀU PHỐI DANH MỤC QUA ICategoryService
    public List<Category> GetCategories() => _categoryService.GetCategories();
    public void SaveCategory(Category category) => _categoryService.SaveCategory(category);
    public (bool Success, string Message) DeleteCategory(int id) => _categoryService.DeleteCategory(id);
    public bool ToggleCategoryActive(int id) => _categoryService.ToggleCategoryActive(id);
    #endregion

    #region 4. ĐIỀU PHỐI ĐƠN HÀNG QUA IOrderService
    public List<Order> GetOrders(string? status = null) => _orderService.GetOrders(status);
    public Order? GetOrderById(int id, string? requesterUserId = null, bool requesterIsAdmin = false, int? guestOrderId = null, string? guestPhone = null)
        => _orderService.GetOrderById(id, requesterUserId, requesterIsAdmin, guestOrderId, guestPhone);
    public bool UpdateOrderStatus(int orderId, string status) => _orderService.UpdateOrderStatus(orderId, status);
    public (bool Success, string Message) CancelOrder(int orderId, bool isClient = true, string? requesterUserId = null, string? requesterPhone = null)
        => _orderService.CancelOrder(orderId, isClient, requesterUserId, requesterPhone);
    public (bool Success, string Message, int OrderId) PlaceOrder(
        string fullName, string phone, string email, string address, string paymentMethod, string? notes, List<CartItem> items, string? userId = null)
        => _orderService.PlaceOrder(fullName, phone, email, address, paymentMethod, notes, items, userId);
    #endregion

    #region 5. QUẢN LÝ TÀI KHOẢN (ACCOUNTS)
    public List<ApplicationUser> GetUsers()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Users.OrderByDescending(u => u.CreatedAt).ToList();
    }

    public bool ToggleUserLock(string userId)
    {
        using var context = _contextFactory.CreateDbContext();
        var user = context.Users.Find(userId);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            context.SaveChanges();
            return true;
        }
        return false;
    }
    #endregion
}
