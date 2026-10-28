using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class OrdersModel : PageModel
{
    private readonly ElectronicStoreDbContext _context;

    public OrdersModel(ElectronicStoreDbContext context)
    {
        _context = context;
    }

    public List<Order> Orders { get; set; } = new();

    public int TotalOrdersCount { get; set; }
    public int PendingOrdersCount { get; set; }
    public int ShippingOrdersCount { get; set; }
    public int CompletedOrdersCount { get; set; }
    public decimal TotalRevenue { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return "Pending";
        return status switch
        {
            "Chờ xử lý" or "Pending" => "Pending",
            "Đã duyệt" or "Confirmed" => "Confirmed",
            "Đang giao hàng" or "Đang giao" or "Shipping" => "Shipping",
            "Hoàn thành" or "Completed" or "Đã giao" or "Hoàn tất" => "Completed",
            "Đã hủy" or "Cancelled" => "Cancelled",
            _ => status
        };
    }

    public async Task OnGetAsync()
    {
        var allOrders = await _context.Orders.ToListAsync();
        TotalOrdersCount = allOrders.Count;
        PendingOrdersCount = allOrders.Count(o => NormalizeStatus(o.OrderStatus) == "Pending");
        ShippingOrdersCount = allOrders.Count(o => NormalizeStatus(o.OrderStatus) == "Shipping");
        CompletedOrdersCount = allOrders.Count(o => NormalizeStatus(o.OrderStatus) == "Completed");
        TotalRevenue = allOrders.Where(o => NormalizeStatus(o.OrderStatus) == "Completed").Sum(o => o.TotalAmount);

        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(StatusFilter))
        {
            switch (StatusFilter)
            {
                case "Pending":
                    query = query.Where(o => o.OrderStatus == "Pending" || o.OrderStatus == "Chờ xử lý");
                    break;
                case "Confirmed":
                    query = query.Where(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Đã duyệt");
                    break;
                case "Shipping":
                    query = query.Where(o => o.OrderStatus == "Shipping" || o.OrderStatus == "Đang giao" || o.OrderStatus == "Đang giao hàng");
                    break;
                case "Completed":
                    query = query.Where(o => o.OrderStatus == "Completed" || o.OrderStatus == "Hoàn thành" || o.OrderStatus == "Đã giao" || o.OrderStatus == "Hoàn tất");
                    break;
                case "Cancelled":
                    query = query.Where(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Đã hủy");
                    break;
                default:
                    query = query.Where(o => o.OrderStatus == StatusFilter);
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLower();
            var cleanKw = kw.Replace("#", "").Replace("es", "").TrimStart('0');
            int.TryParse(cleanKw, out int searchOrderId);

            query = query.Where(o => (searchOrderId > 0 && o.Id == searchOrderId) ||
                                     (o.ReceiverName != null && o.ReceiverName.ToLower().Contains(kw)) ||
                                     (o.ReceiverPhone != null && o.ReceiverPhone.Contains(kw)) ||
                                     (o.ShippingAddress != null && o.ShippingAddress.ToLower().Contains(kw)) ||
                                     (o.User != null && o.User.FullName != null && o.User.FullName.ToLower().Contains(kw)) ||
                                     (o.User != null && o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(kw)));
        }

        Orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng (Duyệt đơn, Đang giao, Hoàn thành, Hủy đơn - tự động hoàn kho khi Hủy)
    /// </summary>
    public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order != null)
        {
            // 1. Nếu chuyển sang Cancelled mà trước đó chưa phải Cancelled -> hoàn lại tồn kho
            if (newStatus == "Cancelled" && order.OrderStatus != "Cancelled")
            {
                foreach (var d in order.OrderDetails)
                {
                    var prod = await _context.Products.FindAsync(d.ProductId);
                    if (prod != null)
                    {
                        prod.Quantity += d.Quantity;
                    }
                }
            }
            // 2. Nếu từ Cancelled đổi lại trạng thái khác -> trừ lại kho
            else if (order.OrderStatus == "Cancelled" && newStatus != "Cancelled")
            {
                foreach (var d in order.OrderDetails)
                {
                    var prod = await _context.Products.FindAsync(d.ProductId);
                    if (prod != null)
                    {
                        prod.Quantity = Math.Max(0, prod.Quantity - d.Quantity);
                    }
                }
            }

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();
            AlertMessage = $"Đã cập nhật trạng thái đơn hàng #ES{order.Id:D7} sang '{newStatus}'!";
            AlertType = "success";
        }

        return RedirectToPage(new { StatusFilter, SearchKeyword });
    }
}
