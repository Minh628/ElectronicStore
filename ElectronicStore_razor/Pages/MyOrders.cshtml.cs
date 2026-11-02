using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class MyOrdersModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IAccountService _accountService;

    public MyOrdersModel(IOrderService orderService, IAccountService accountService)
    {
        _orderService = orderService;
        _accountService = accountService;
    }

    public List<Order> Orders { get; set; } = new();
    public List<Order> AllOrders { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LookupPhone { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "All";

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public static string NormalizeStatus(string? status) => OrderService.NormalizeStatusStatic(status);

    public async Task OnGetAsync()
    {
        var currentUser = (User.Identity != null && User.Identity.IsAuthenticated) ? await _accountService.GetCurrentUserAsync(User) : null;
        var currentUserId = currentUser?.Id;
        var userPhone = currentUser?.PhoneNumber;

        if (string.IsNullOrWhiteSpace(LookupPhone) && currentUserId == null)
        {
            Orders = new List<Order>();
            AllOrders = new List<Order>();
            return;
        }

        var (allOrders, filteredOrders) = await _orderService.GetCustomerOrdersOverviewAsync(currentUserId, userPhone, LookupPhone, Status);
        AllOrders = allOrders;
        Orders = filteredOrders;
    }

    /// <summary>
    /// Xử lý Khách hàng HỦY ĐƠN HÀNG và HOÀN TRẢ TỒN KHO thông qua OrderService
    /// </summary>
    public async Task<IActionResult> OnPostCancelOrderAsync(int orderId, string? status)
    {
        if (!string.IsNullOrEmpty(status))
        {
            Status = status;
        }

        var currentUser = (User.Identity != null && User.Identity.IsAuthenticated) ? await _accountService.GetCurrentUserAsync(User) : null;
        var result = await _orderService.CancelCustomerOrderAsync(orderId, currentUser?.Id, currentUser?.PhoneNumber, LookupPhone);

        if (result.Success)
        {
            AlertMessage = result.Message;
            AlertType = "success";
        }
        else
        {
            AlertMessage = result.Message;
            AlertType = "danger";
        }

        return RedirectToPage(new { LookupPhone, Status });
    }
}
