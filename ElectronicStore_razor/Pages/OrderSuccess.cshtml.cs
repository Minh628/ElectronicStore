using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages;

public class OrderSuccessModel : PageModel
{
    private readonly ElectronicStoreDbContext _context;

    public OrderSuccessModel(ElectronicStoreDbContext context)
    {
        _context = context;
    }

    public Order? Order { get; set; }

    // Hỗ trợ nhận cả 2 dạng query string: ?orderId=123 hoặc ?id=123
    public async Task<IActionResult> OnGetAsync(int? orderId, int? id)
    {
        var targetId = orderId ?? id ?? 0;
        Order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == targetId);

        return Page();
    }
}
