using Microsoft.EntityFrameworkCore;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services.Admin;

public class AdminOrderService : IAdminOrderService
{
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;

    public AdminOrderService(IDbContextFactory<ElectronicStoreDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public static string NormalizeStatusStatic(string? status)
    {
        if (string.IsNullOrEmpty(status)) return "Pending";
        var s = status.Trim().ToLowerInvariant();
        return s switch
        {
            "chờ xử lý" or "chờ xác nhận" or "pending" => "Pending",
            "đã duyệt" or "confirmed" => "Confirmed",
            "đang giao hàng" or "đang giao" or "shipping" => "Shipping",
            "hoàn thành" or "đã giao" or "completed" or "delivered" => "Completed",
            "đã hủy" or "cancelled" or "canceled" or "hủy" => "Cancelled",
            _ => status.Trim()
        };
    }

    public string NormalizeStatus(string? status) => NormalizeStatusStatic(status);

    public OrderAdminResult GetAdminOrders(string? statusFilter, string? search)
    {
        using var context = _contextFactory.CreateDbContext();

        var allOrders = context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsNoTracking()
            .ToList();

        var result = new OrderAdminResult
        {
            TotalOrdersCount = allOrders.Count,
            PendingCount = allOrders.Count(o => NormalizeStatusStatic(o.OrderStatus) == "Pending"),
            ShippingCount = allOrders.Count(o => NormalizeStatusStatic(o.OrderStatus) == "Shipping"),
            CompletedCount = allOrders.Count(o => NormalizeStatusStatic(o.OrderStatus) == "Completed"),
            TotalRevenue = allOrders
                .Where(o => NormalizeStatusStatic(o.OrderStatus) == "Completed")
                .Sum(o => o.TotalAmount)
        };

        var query = allOrders.AsEnumerable();

        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
        {
            var targetStatus = NormalizeStatusStatic(statusFilter);
            query = query.Where(o => NormalizeStatusStatic(o.OrderStatus) == targetStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            var cleanKw = kw.Replace("#", "").Replace("es", "").TrimStart('0');
            int.TryParse(cleanKw, out int searchOrderId);

            query = query.Where(o => (searchOrderId > 0 && o.Id == searchOrderId) ||
                                     (o.ReceiverName != null && o.ReceiverName.ToLower().Contains(kw)) ||
                                     (o.ReceiverPhone != null && o.ReceiverPhone.Contains(kw)) ||
                                     (o.ShippingAddress != null && o.ShippingAddress.ToLower().Contains(kw)) ||
                                     (o.User != null && o.User.FullName != null && o.User.FullName.ToLower().Contains(kw)) ||
                                     (o.User != null && o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(kw)));
        }

        result.Orders = query.OrderByDescending(o => o.OrderDate).ToList();
        return result;
    }

    public Order? GetOrderById(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefault(o => o.Id == id);
    }

    public (bool Success, string Message) UpdateOrderStatus(int orderId, string newStatus)
    {
        using var context = _contextFactory.CreateDbContext();
        var order = context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefault(o => o.Id == orderId);

        if (order == null)
        {
            return (false, "Không tìm thấy đơn hàng!");
        }

        var oldNormalized = NormalizeStatusStatic(order.OrderStatus);
        var newNormalized = NormalizeStatusStatic(newStatus);

        // Hoàn trả kho khi đơn hàng bị hủy
        if (oldNormalized != "Cancelled" && newNormalized == "Cancelled")
        {
            foreach (var detail in order.OrderDetails)
            {
                var prod = context.Products.Find(detail.ProductId);
                if (prod != null)
                {
                    prod.Quantity += detail.Quantity;
                }
            }
        }
        // Trừ lại kho khi khôi phục đơn hàng từ trạng thái Hủy
        else if (oldNormalized == "Cancelled" && newNormalized != "Cancelled")
        {
            foreach (var detail in order.OrderDetails)
            {
                var prod = context.Products.Find(detail.ProductId);
                if (prod != null)
                {
                    if (prod.Quantity < detail.Quantity)
                    {
                        return (false, $"Không thể khôi phục đơn hàng! Sản phẩm '{prod.Name}' chỉ còn {prod.Quantity} chiếc trong kho (cần {detail.Quantity} chiếc).");
                    }
                    prod.Quantity -= detail.Quantity;
                }
            }
        }

        order.OrderStatus = newNormalized;
        context.SaveChanges();

        var displayStatus = newNormalized switch
        {
            "Confirmed" => "Đã duyệt",
            "Shipping" => "Đang giao hàng",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            _ => newNormalized
        };

        return (true, $"Đã cập nhật trạng thái đơn hàng #ES{orderId:D7} sang '{displayStatus}'!");
    }
}
