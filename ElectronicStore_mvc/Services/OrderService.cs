using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

/// <summary>
/// Triển khai nghiệp vụ đơn hàng End-User
/// </summary>
public class OrderService : IOrderService
{
    private readonly ElectronicStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderService(ElectronicStoreDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public static string NormalizeStatusStatic(string? status) => status switch
    {
        "Chờ xử lý" or "Pending" => "Pending",
        "Đã duyệt" or "Confirmed" => "Confirmed",
        "Đang giao" or "Đang giao hàng" or "Shipping" => "Shipping",
        "Hoàn thành" or "Đã giao" or "Hoàn tất" or "Completed" => "Completed",
        "Đã hủy" or "Cancelled" => "Cancelled",
        _ => status ?? string.Empty
    };

    public string NormalizeStatus(string? status) => NormalizeStatusStatic(status);

    public async Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(
        string fullName, string phoneNumber, string? email, string shippingAddress,
        string paymentMethod, string? notes, List<CartItem> cart, string? currentUserId)
    {
        if (paymentMethod is not ("COD" or "BankTransfer" or "VNPAY" or "MOMO"))
        {
            return (false, "Phương thức thanh toán không hợp lệ.", 0);
        }

        if (cart == null || cart.Count == 0)
        {
            return (false, "Giỏ hàng trống, không thể tiến hành đặt hàng!", 0);
        }

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) ||
            string.IsNullOrWhiteSpace(shippingAddress) || shippingAddress.Trim() == "Chưa cập nhật địa chỉ")
        {
            return (false, "Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Địa chỉ giao hàng!", 0);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            foreach (var item in cart)
            {
                var prod = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (prod == null || !prod.IsActive || (prod.Category != null && !prod.Category.IsActive))
                {
                    await transaction.RollbackAsync();
                    return (false, $"Sản phẩm '{item.ProductName}' hiện không còn kinh doanh!", 0);
                }

                if (prod.Quantity < item.Quantity)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Sản phẩm '{prod.Name}' chỉ còn {prod.Quantity} chiếc trong kho (bạn đặt {item.Quantity} chiếc)!", 0);
                }

                item.ProductName = prod.Name;
                item.UnitPrice = prod.Price;
            }

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var user = await _userManager.FindByIdAsync(currentUserId);
                if (user != null)
                {
                    bool userUpdated = false;
                    if (string.IsNullOrEmpty(user.FullName))
                    {
                        user.FullName = fullName.Trim();
                        userUpdated = true;
                    }
                    if (string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        user.PhoneNumber = phoneNumber.Trim();
                        userUpdated = true;
                    }
                    if (string.IsNullOrEmpty(user.Address) || user.Address == "Chưa cập nhật địa chỉ")
                    {
                        user.Address = shippingAddress.Trim();
                        userUpdated = true;
                    }
                    if (userUpdated)
                    {
                        await _userManager.UpdateAsync(user);
                    }
                }
            }

            var order = new Order
            {
                UserId = currentUserId,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(item => item.TotalPrice),
                OrderStatus = "Pending",
                PaymentMethod = paymentMethod ?? "COD",
                ReceiverName = fullName.Trim(),
                ReceiverPhone = phoneNumber.Trim(),
                ShippingAddress = shippingAddress.Trim(),
                Notes = notes?.Trim()
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                };
                _context.OrderDetails.Add(orderDetail);

                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Quantity = Math.Max(0, product.Quantity - item.Quantity);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Đặt hàng thành công!", order.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi xử lý đặt hàng: {ex.Message}", 0);
        }
    }

    public async Task<List<Order>> GetCustomerOrdersAsync(string? userId, string? userPhone, string? lookupPhone)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(lookupPhone))
        {
            var phoneTrim = lookupPhone.Trim();
            query = query.Where(o => o.ReceiverPhone == phoneTrim &&
                                     (string.IsNullOrEmpty(o.UserId) || (userId != null && o.UserId == userId)));
        }
        else if (!string.IsNullOrEmpty(userId))
        {
            var phoneTrim = userPhone?.Trim();
            query = query.Where(o => o.UserId == userId ||
                                     (string.IsNullOrEmpty(o.UserId) && !string.IsNullOrEmpty(phoneTrim) && o.ReceiverPhone == phoneTrim));
        }
        else
        {
            return new List<Order>();
        }

        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    public async Task<Order?> GetOrderForViewerAsync(
        int orderId, string? userId, string? userPhone, bool isAdmin, int? lastGuestOrderId, string? lookupPhone)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;
        if (isAdmin) return order;

        var effectivePhone = !string.IsNullOrWhiteSpace(lookupPhone) ? lookupPhone.Trim() : userPhone?.Trim();
        bool ownsOrder = (!string.IsNullOrEmpty(userId) && order.UserId == userId) ||
                         (lastGuestOrderId.HasValue && lastGuestOrderId.Value == orderId) ||
                         (string.IsNullOrEmpty(order.UserId) && !string.IsNullOrWhiteSpace(effectivePhone) && order.ReceiverPhone == effectivePhone);

        return ownsOrder ? order : null;
    }

    public async Task<(bool Success, string Message)> CancelCustomerOrderAsync(
        int orderId, string? userId, string? userPhone, string? lookupPhone)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return (false, "Không tìm thấy đơn hàng cần hủy.");
        }

        if (!string.IsNullOrEmpty(userId))
        {
            var effectivePhone = !string.IsNullOrWhiteSpace(lookupPhone) ? lookupPhone.Trim() : userPhone?.Trim();
            bool isOwner = order.UserId == userId ||
                           (string.IsNullOrEmpty(order.UserId) && !string.IsNullOrWhiteSpace(effectivePhone) && order.ReceiverPhone == effectivePhone);
            if (!isOwner)
            {
                return (false, "Bạn không có quyền thao tác trên đơn hàng này!");
            }
        }
        else if (!string.IsNullOrWhiteSpace(lookupPhone))
        {
            if (order.ReceiverPhone != lookupPhone.Trim())
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
            return (false, "Yêu cầu không hợp lệ!");
        }

        var currentStatus = NormalizeStatus(order.OrderStatus);
        if (currentStatus != "Pending" && currentStatus != "Confirmed")
        {
            return (false, "Đơn hàng đang giao hoặc đã hoàn tất/đã hủy, không thể hủy.");
        }

        foreach (var detail in order.OrderDetails)
        {
            var product = await _context.Products.FindAsync(detail.ProductId);
            if (product != null)
            {
                product.Quantity += detail.Quantity;
            }
        }

        order.OrderStatus = "Cancelled";
        await _context.SaveChangesAsync();

        return (true, $"Đã hủy thành công đơn hàng #ES{order.Id:D7}. Số lượng sản phẩm đã được tự động hoàn lại vào kho.");
    }
}
