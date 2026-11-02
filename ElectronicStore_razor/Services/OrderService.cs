using System.Data;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

/// <summary>
/// Service quản lý đơn hàng dành riêng cho End-User (Khách mua hàng)
/// </summary>
public class OrderService : IOrderService
{
    private readonly ElectronicStoreDbContext _context;

    public OrderService(ElectronicStoreDbContext context)
    {
        _context = context;
    }

    public static string NormalizeStatusStatic(string? status)
    {
        if (string.IsNullOrEmpty(status)) return "Pending";
        var s = status.Trim().ToLowerInvariant();
        return s switch
        {
            "pending" or "chờ xử lý" or "chờ xác nhận" or "cho xu ly" => "Pending",
            "confirmed" or "đã duyệt" or "da duyet" => "Confirmed",
            "shipping" or "đang giao" or "đang giao hàng" or "dang giao" => "Shipping",
            "completed" or "delivered" or "hoàn thành" or "đã giao" or "hoàn tất" or "da giao" => "Completed",
            "cancelled" or "canceled" or "đã hủy" or "da huy" or "hủy" => "Cancelled",
            _ => status.Trim()
        };
    }

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

        var productIds = cart.Select(c => c.ProductId).ToList();
        var dbProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var item in cart)
        {
            var prod = dbProducts.FirstOrDefault(p => p.Id == item.ProductId);
            if (prod == null || !prod.IsActive || (prod.Category != null && !prod.Category.IsActive))
            {
                return (false, $"Sản phẩm '{item.ProductName}' hiện không còn kinh doanh!", 0);
            }

            if (prod.Quantity < item.Quantity)
            {
                return (false, $"Sản phẩm '{prod.Name}' chỉ còn {prod.Quantity} chiếc trong kho (bạn đặt {item.Quantity} chiếc)!", 0);
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
            Notes = notes?.Trim(),
            OrderDetails = new List<OrderDetail>()
        };

        foreach (var item in cart)
        {
            var prod = dbProducts.First(p => p.Id == item.ProductId);
            prod.Quantity -= item.Quantity;

            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = prod.Id,
                Quantity = item.Quantity,
                UnitPrice = prod.Price,
                TotalPrice = prod.Price * item.Quantity
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return (true, "Đặt hàng thành công!", order.Id);
    }

    public async Task<List<Order>> GetCustomerOrdersAsync(string? userId, string? userPhone, string? lookupPhone)
    {
        var query = _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(o => o.UserId == userId || (!string.IsNullOrEmpty(userPhone) && o.ReceiverPhone == userPhone));
        }
        else if (!string.IsNullOrEmpty(lookupPhone))
        {
            query = query.Where(o => o.ReceiverPhone == lookupPhone.Trim());
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
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        if (isAdmin) return order;

        if (!string.IsNullOrEmpty(userId) && (order.UserId == userId || (!string.IsNullOrEmpty(userPhone) && order.ReceiverPhone == userPhone)))
        {
            return order;
        }

        if (!string.IsNullOrEmpty(lookupPhone) && order.ReceiverPhone == lookupPhone.Trim())
        {
            return order;
        }

        if (lastGuestOrderId.HasValue && lastGuestOrderId.Value == orderId)
        {
            return order;
        }

        return null;
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

        bool isOwner = (!string.IsNullOrEmpty(userId) && (order.UserId == userId || (!string.IsNullOrEmpty(userPhone) && order.ReceiverPhone == userPhone))) ||
                       (!string.IsNullOrEmpty(lookupPhone) && order.ReceiverPhone == lookupPhone.Trim());

        if (!isOwner)
        {
            return (false, "Bạn không có quyền thao tác trên đơn hàng này.");
        }

        var currentStatus = NormalizeStatusStatic(order.OrderStatus);
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
