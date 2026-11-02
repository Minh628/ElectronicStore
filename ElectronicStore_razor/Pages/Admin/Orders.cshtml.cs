using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services.Admin;

namespace ElectronicStore_razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class OrdersModel : PageModel
{
    private readonly IAdminOrderService _orderService;

    public OrdersModel(IAdminOrderService orderService)
    {
        _orderService = orderService;
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

    public static string NormalizeStatus(string? status) => AdminOrderService.NormalizeStatusStatic(status);

    public async Task OnGetAsync()
    {
        var result = await _orderService.GetAdminOrdersAsync(StatusFilter, SearchKeyword);
        TotalOrdersCount = result.TotalOrdersCount;
        PendingOrdersCount = result.PendingCount;
        ShippingOrdersCount = result.ShippingCount;
        CompletedOrdersCount = result.CompletedCount;
        TotalRevenue = result.TotalRevenue;
        Orders = result.Orders;
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng thông qua OrderService
    /// </summary>
    public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus)
    {
        var result = await _orderService.UpdateOrderStatusAsync(orderId, newStatus);
        AlertMessage = result.Message;
        AlertType = result.Success ? "success" : "danger";
        return RedirectToPage(new { StatusFilter, SearchKeyword });
    }
}
