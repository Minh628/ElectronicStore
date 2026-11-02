using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services.Admin;

public class AdminOrderService : IAdminOrderService
{
    private readonly ElectronicStoreDbContext _context;

    public AdminOrderService(ElectronicStoreDbContext context)
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

    public async Task<OrderAdminResult> GetAdminOrdersAsync(string? status, string? search)
    {
        var allOrders = await _context.Orders.ToListAsync();

        int totalOrdersCount = allOrders.Count;
        int pendingCount = allOrders.Count(o => NormalizeStatusStatic(o.OrderStatus) == "Pending");
        int shippingCount = allOrders.Count(o => NormalizeStatusStatic(o.OrderStatus) == "Shipping");
        int completedCount = allOrders.Count(o => NormalizeStatusStatic(o.OrderStatus) == "Completed");
        decimal totalRevenue = allOrders
            .Where(o => NormalizeStatusStatic(o.OrderStatus) == "Completed")
            .Sum(o => o.TotalAmount);

        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        var normalizedStatus = NormalizeStatusStatic(status);
        if (!string.IsNullOrWhiteSpace(normalizedStatus) && normalizedStatus != "All")
        {
            query = query.Where(o => o.OrderStatus == normalizedStatus ||
                                     (normalizedStatus == "Pending" && o.OrderStatus == "Chờ xử lý") ||
                                     (normalizedStatus == "Confirmed" && o.OrderStatus == "Đã duyệt") ||
                                     (normalizedStatus == "Shipping" && (o.OrderStatus == "Đang giao" || o.OrderStatus == "Đang giao hàng")) ||
                                     (normalizedStatus == "Completed" && (o.OrderStatus == "Hoàn thành" || o.OrderStatus == "Đã giao" || o.OrderStatus == "Hoàn tất")) ||
                                     (normalizedStatus == "Cancelled" && o.OrderStatus == "Đã hủy"));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(o => o.Id.ToString().Contains(kw) ||
                                     o.ReceiverName.ToLower().Contains(kw) ||
                                     o.ReceiverPhone.Contains(kw));
        }

        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

        return new OrderAdminResult
        {
            Orders = orders,
            TotalOrdersCount = totalOrdersCount,
            PendingCount = pendingCount,
            ShippingCount = shippingCount,
            CompletedCount = completedCount,
            TotalRevenue = totalRevenue
        };
    }

    public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return (false, "Không tìm thấy đơn hàng!");
        }

        var oldNormalized = NormalizeStatusStatic(order.OrderStatus);
        var newNormalized = NormalizeStatusStatic(newStatus);

        if (oldNormalized != "Cancelled" && newNormalized == "Cancelled")
        {
            foreach (var detail in order.OrderDetails)
            {
                var prod = await _context.Products.FindAsync(detail.ProductId);
                if (prod != null)
                {
                    prod.Quantity += detail.Quantity;
                }
            }
        }
        else if (oldNormalized == "Cancelled" && newNormalized != "Cancelled")
        {
            foreach (var detail in order.OrderDetails)
            {
                var prod = await _context.Products.FindAsync(detail.ProductId);
                if (prod != null)
                {
                    if (prod.Quantity < detail.Quantity)
                    {
                        return (false, $"Không thể khôi phục đơn hàng! Sản phẩm '{prod.Name}' chỉ còn {prod.Quantity} chiếc trong kho (đơn hàng cần {detail.Quantity} chiếc).");
                    }
                    prod.Quantity -= detail.Quantity;
                }
            }
        }

        order.OrderStatus = newNormalized;
        await _context.SaveChangesAsync();

        return (true, $"Đã cập nhật trạng thái đơn hàng #{order.Id} thành: {newNormalized}");
    }
}
