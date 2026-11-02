using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class OrderSuccessModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IAccountService _accountService;

    public OrderSuccessModel(
        IOrderService orderService,
        IAccountService accountService)
    {
        _orderService = orderService;
        _accountService = accountService;
    }

    public Order? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(int? orderId, int? id, string? phone)
    {
        var targetId = orderId ?? id ?? HttpContext.Session.GetInt32("LastGuestOrderId") ?? 0;
        var user = (User.Identity != null && User.Identity.IsAuthenticated) ? await _accountService.GetCurrentUserAsync(User) : null;
        bool isAdmin = User.IsInRole("Admin");
        int? lastGuestId = HttpContext.Session.GetInt32("LastGuestOrderId");

        Order = await _orderService.GetOrderForViewerAsync(
            targetId, user?.Id, user?.PhoneNumber, isAdmin, lastGuestId, phone);

        return Page();
    }
}
