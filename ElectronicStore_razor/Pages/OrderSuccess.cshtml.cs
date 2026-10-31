using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages;

public class OrderSuccessModel : PageModel
{
    private readonly ElectronicStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderSuccessModel(
        ElectronicStoreDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Order? Order { get; set; }

    // Hỗ trợ nhận cả 2 dạng query string: ?orderId=123 hoặc ?id=123 (và fallback về đơn vừa đặt trong phiên)
    public async Task<IActionResult> OnGetAsync(int? orderId, int? id, string? phone)
    {
        var targetId = orderId ?? id ?? HttpContext.Session.GetInt32("LastGuestOrderId") ?? 0;
        Order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == targetId);

        if (Order != null && !User.IsInRole("Admin"))
        {
            var user = await _userManager.GetUserAsync(User);
            var effectivePhone = !string.IsNullOrWhiteSpace(phone) ? phone.Trim() : user?.PhoneNumber?.Trim();
            var ownsOrder = (user != null && Order.UserId == user.Id) ||
                            HttpContext.Session.GetInt32("LastGuestOrderId") == targetId ||
                            (string.IsNullOrEmpty(Order.UserId) && !string.IsNullOrWhiteSpace(effectivePhone) && Order.ReceiverPhone == effectivePhone);
            if (!ownsOrder)
            {
                Order = null;
            }
        }

        return Page();
    }
}
