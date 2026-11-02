using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class MyOrdersModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyOrdersModel(IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
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
        var currentUser = (User.Identity != null && User.Identity.IsAuthenticated) ? await _userManager.GetUserAsync(User) : null;
        var currentUserId = currentUser?.Id;
        var userPhone = currentUser?.PhoneNumber;

        if (string.IsNullOrWhiteSpace(LookupPhone) && currentUserId == null)
        {
            Orders = new List<Order>();
            AllOrders = new List<Order>();
            return;
        }

        var allOrders = await _orderService.GetCustomerOrdersAsync(currentUserId, userPhone, LookupPhone);
        AllOrders = allOrders;
        Orders = string.IsNullOrWhiteSpace(Status) || Status == "All"
            ? allOrders
            : allOrders.Where(o => _orderService.NormalizeStatus(o.OrderStatus) == _orderService.NormalizeStatus(Status)).ToList();
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

        var currentUser = (User.Identity != null && User.Identity.IsAuthenticated) ? await _userManager.GetUserAsync(User) : null;
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
