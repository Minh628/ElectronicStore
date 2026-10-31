using Microsoft.EntityFrameworkCore;
using System.Data;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Service kết nối và thao tác trực tiếp với cơ sở dữ liệu SQL Server (ElectronicStoreDb)
/// </summary>
public class ProductService : IProductService
{
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;

    public ProductService(IDbContextFactory<ElectronicStoreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region 1. SẢN PHẨM KHÁCH HÀNG
    public List<Product> GetAllProducts(int? categoryId = null, string? search = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Products.Include(p => p.Category)
            .Where(p => p.IsActive && p.Category.IsActive).AsQueryable();

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
            .FirstOrDefault(p => p.Id == id && p.IsActive && p.Category.IsActive);
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

    #region 2. QUẢN TRỊ SẢN PHẨM
    public List<Product> GetAllAdminProducts()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Products.Include(p => p.Category).OrderByDescending(p => p.Id).ToList();
    }

    public void AddProduct(Product product)
    {
        using var context = _contextFactory.CreateDbContext();
        product.IsActive = true;
        if (product.CategoryId == 0) product.CategoryId = 1;
        if (string.IsNullOrWhiteSpace(product.Brand)) product.Brand = "Chính Hãng";
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
            existing.CategoryId = product.CategoryId;
            existing.Brand = product.Brand;
            existing.Price = product.Price;
            existing.Quantity = product.Quantity;
            existing.WarrantyMonths = product.WarrantyMonths;
            existing.ImageUrl = product.ImageUrl;
            existing.Specifications = product.Specifications;
            existing.Description = product.Description;
            context.SaveChanges();
            return true;
        }
        return false;
    }

    public bool DeleteProduct(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var product = context.Products.Include(p => p.OrderDetails).FirstOrDefault(p => p.Id == id);
        if (product != null)
        {
            // Nếu sản phẩm đã phát sinh đơn hàng thì ẩn (soft delete), nếu chưa thì xóa hẳn
            if (product.OrderDetails.Any())
            {
                product.IsActive = false;
            }
            else
            {
                context.Products.Remove(product);
            }
            context.SaveChanges();
            return true;
        }
        return false;
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

    #region 3. QUẢN LÝ DANH MỤC
    public List<Category> GetCategories()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Categories.Include(c => c.Products).OrderBy(c => c.Id).ToList();
    }

    public void SaveCategory(Category category)
    {
        using var context = _contextFactory.CreateDbContext();
        if (category.Id == 0)
        {
            category.IsActive = true;
            context.Categories.Add(category);
        }
        else
        {
            var existing = context.Categories.Find(category.Id);
            if (existing != null)
            {
                existing.Name = category.Name;
                existing.Description = category.Description;
            }
        }
        context.SaveChanges();
    }

    public (bool Success, string Message) DeleteCategory(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var category = context.Categories.Include(c => c.Products).FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            return (false, "Không tìm thấy danh mục!");
        }

        if (category.Products.Any())
        {
            return (false, $"Không thể xóa danh mục '{category.Name}' vì đang có {category.Products.Count} sản phẩm liên kết! Hãy chuyển hoặc xóa các sản phẩm trước.");
        }

        context.Categories.Remove(category);
        context.SaveChanges();
        return (true, $"Đã xóa danh mục: {category.Name}");
    }
    #endregion

    #region 4. QUẢN LÝ ĐƠN HÀNG
    public List<Order> GetOrders(string? status = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.OrderStatus == status);
        }

        return query.OrderByDescending(o => o.OrderDate).ToList();
    }

    public Order? GetOrderById(int id, string? requesterUserId = null, bool requesterIsAdmin = false, int? guestOrderId = null, string? guestPhone = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var query = context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Where(o => o.Id == id);

        if (requesterIsAdmin)
        {
            return query.FirstOrDefault();
        }

        return query.FirstOrDefault(o =>
            (requesterUserId != null && o.UserId == requesterUserId) ||
            (o.UserId == null && (guestOrderId == o.Id ||
                                  (!string.IsNullOrWhiteSpace(guestPhone) && o.ReceiverPhone == guestPhone.Trim()))));
    }

    public bool UpdateOrderStatus(int orderId, string status)
    {
        status = NormalizeStatus(status);
        if (status is not ("Pending" or "Confirmed" or "Shipping" or "Completed" or "Cancelled"))
        {
            return false;
        }

        using var context = _contextFactory.CreateDbContext();
        var order = context.Orders.Include(o => o.OrderDetails).FirstOrDefault(o => o.Id == orderId);
        if (order != null)
        {
            // Nếu chuyển trạng thái sang Cancelled từ trạng thái khác -> hoàn lại kho
            if (status == "Cancelled" && order.OrderStatus != "Cancelled")
            {
                foreach (var detail in order.OrderDetails)
                {
                    var product = context.Products.Find(detail.ProductId);
                    if (product != null)
                    {
                        product.Quantity += detail.Quantity;
                    }

                }
            }
            else if (order.OrderStatus == "Cancelled" && status != "Cancelled")
            {
                foreach (var detail in order.OrderDetails)
                {
                    var product = context.Products.Find(detail.ProductId);
                    if (product != null)
                    {
                        product.Quantity = Math.Max(0, product.Quantity - detail.Quantity);
                    }
                }
            }

            order.OrderStatus = status;
            context.SaveChanges();
            return true;
        }
        return false;
    }

    private static string NormalizeStatus(string? status) => status switch
    {
        "Chờ xử lý" or "Pending" => "Pending",
        "Đã duyệt" or "Confirmed" => "Confirmed",
        "Đang giao" or "Đang giao hàng" or "Shipping" => "Shipping",
        "Hoàn thành" or "Đã giao" or "Hoàn tất" or "Completed" => "Completed",
        "Đã hủy" or "Cancelled" => "Cancelled",
        _ => status ?? string.Empty
    };

    public (bool Success, string Message) CancelOrder(int orderId, bool isClient = true, string? requesterUserId = null, string? requesterPhone = null)
    {
        using var context = _contextFactory.CreateDbContext();
        var order = context.Orders.Include(o => o.OrderDetails).FirstOrDefault(o => o.Id == orderId);
        if (order == null) return (false, "Không tìm thấy đơn hàng cần hủy!");

        // Kiểm tra quyền sở hữu đơn hàng khi khách hàng hủy (Chống tấn công IDOR)
        if (isClient)
        {
            if (!string.IsNullOrEmpty(requesterUserId))
            {
                if (order.UserId != requesterUserId)
                {
                    return (false, "Bạn không có quyền thao tác trên đơn hàng này!");
                }
            }
            else if (!string.IsNullOrWhiteSpace(requesterPhone))
            {
                if (order.ReceiverPhone != requesterPhone.Trim())
                {
                    return (false, "Số điện thoại tra cứu không khớp với đơn hàng!");
                }
                if (!string.IsNullOrEmpty(order.UserId))
                {
                    return (false, "Đơn hàng này thuộc tài khoản thành viên. Vui lòng đăng nhập tài khoản để hủy đơn!");
                }
            }
            else
            {
                return (false, "Yêu cầu hủy đơn không hợp lệ!");
            }
        }

        // Khách hàng chỉ được hủy trước khi Admin giao hàng (Pending hoặc Confirmed)
        if (isClient && order.OrderStatus != "Pending" && order.OrderStatus != "Confirmed")
        {
            return (false, "Đơn hàng đang trong quá trình vận chuyển hoặc đã hoàn tất, không thể hủy!");
        }

        if (order.OrderStatus == "Cancelled")
        {
            return (false, "Đơn hàng này đã được hủy trước đó!");
        }

        // Hoàn lại số lượng tồn kho cho các sản phẩm
        foreach (var detail in order.OrderDetails)
        {
            var product = context.Products.Find(detail.ProductId);
            if (product != null)
            {
                product.Quantity += detail.Quantity;
            }
        }

        order.OrderStatus = "Cancelled";
        context.SaveChanges();
        return (true, $"Đã hủy thành công đơn hàng #ES{order.Id:D7}. Số lượng sản phẩm đã được hoàn lại kho.");
    }

    public (bool Success, string Message, int OrderId) PlaceOrder(string fullName, string phone, string email, string address, string paymentMethod, string? notes, List<CartItem> items, string? userId = null)
    {
        if (paymentMethod is not ("COD" or "BankTransfer" or "VNPAY" or "MOMO"))
        {
            return (false, "Phương thức thanh toán không hợp lệ.", 0);
        }

        // 1. Kiểm tra thông tin người nhận
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address))
        {
            return (false, "Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Địa chỉ giao hàng!", 0);
        }

        // 2. Kiểm tra giỏ hàng
        if (items == null || !items.Any())
        {
            return (false, "Giỏ hàng rỗng!", 0);
        }

        using var context = _contextFactory.CreateDbContext();
        using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);

        // 3. Kiểm tra tính khả dụng và tồn kho thực tế của từng sản phẩm (tránh bán khống)
        foreach (var item in items)
        {
            var prod = context.Products.Find(item.ProductId);
            if (prod == null || !prod.IsActive)
            {
                return (false, $"Sản phẩm '{item.ProductName}' hiện không còn kinh doanh!", 0);
            }
            if (prod.Quantity < item.Quantity)
            {
                return (false, $"Sản phẩm '{prod.Name}' chỉ còn {prod.Quantity} chiếc trong kho (bạn đặt {item.Quantity} chiếc)!", 0);
            }

            item.ProductName = prod.Name;
            item.UnitPrice = prod.Price;
        }

        // 4. Tạo đơn hàng mới liên kết trực tiếp với UserId và thông tin người nhận
        var order = new Order
        {
            UserId = userId,
            ReceiverName = fullName.Trim(),
            ReceiverPhone = phone.Trim(),
            OrderDate = DateTime.Now,
            TotalAmount = items.Sum(i => i.TotalPrice),
            OrderStatus = "Pending",
            ShippingAddress = address.Trim(),
            PaymentMethod = paymentMethod ?? "COD",
            Notes = notes
        };
        context.Orders.Add(order);
        context.SaveChanges();

        // 5. Thêm chi tiết đơn hàng và trừ kho an toàn
        foreach (var item in items)
        {
            var detail = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            };
            context.OrderDetails.Add(detail);

            var product = context.Products.Find(item.ProductId);
            if (product != null)
            {
                product.Quantity = Math.Max(0, product.Quantity - item.Quantity);
            }
        }
        context.SaveChanges();
        transaction.Commit();

        return (true, "Đặt hàng thành công!", order.Id);
    }
    #endregion

    #region 5. QUẢN LÝ TÀI KHOẢN
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
