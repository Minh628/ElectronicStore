using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages;

public class MyOrdersModel : PageModel
{
    private readonly ElectronicStoreDbContext _context;

    public MyOrdersModel(ElectronicStoreDbContext context)
    {
        _context = context;
    }

    public List<Order> Orders { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LookupPhone { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "All";

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var username = User.Identity.Name;
            query = query.Where(o => o.User != null && o.User.UserName == username);
        }
        else if (!string.IsNullOrWhiteSpace(LookupPhone))
        {
            query = query.Where(o => o.ReceiverPhone == LookupPhone.Trim());
        }
        else
        {
            Orders = new List<Order>();
            return;
        }

        Orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    /// <summary>
    /// Xử lý Khách hàng HỦY ĐƠN HÀNG và HOÀN TRẢ TỒN KHO
    /// Điều kiện: Chỉ hủy khi đơn ở trạng thái Pending hoặc Confirmed (trước bước Giao hàng Shipping)
    /// </summary>
    public async Task<IActionResult> OnPostCancelOrderAsync(int orderId, string? status)
    {
        if (!string.IsNullOrEmpty(status))
        {
            Status = status;
        }

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            AlertMessage = "Không tìm thấy đơn hàng cần hủy!";
            AlertType = "danger";
            return RedirectToPage(new { LookupPhone, Status });
        }

        // Kiểm tra quyền thao tác (Bảo mật chống tấn công IDOR)
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (order.User?.UserName != User.Identity.Name)
            {
                AlertMessage = "Bạn không có quyền thao tác trên đơn hàng này!";
                AlertType = "danger";
                return RedirectToPage(new { LookupPhone, Status });
            }
        }
        else if (!string.IsNullOrWhiteSpace(LookupPhone))
        {
            if (order.ReceiverPhone != LookupPhone.Trim())
            {
                AlertMessage = "Số điện thoại tra cứu không khớp với đơn hàng!";
                AlertType = "danger";
                return RedirectToPage(new { LookupPhone, Status });
            }

            // Nếu đơn này thuộc về tài khoản đã đăng ký thành viên nhưng đang tra cứu ẩn danh: yêu cầu đăng nhập
            if (!string.IsNullOrEmpty(order.UserId))
            {
                AlertMessage = "Đơn hàng này thuộc tài khoản thành viên. Vui lòng đăng nhập tài khoản để hủy đơn!";
                AlertType = "danger";
                return RedirectToPage(new { LookupPhone, Status });
            }
        }
        else
        {
            // Chặn đứng hành vi giả mạo request hủy đơn khi không đăng nhập và không có SĐT
            AlertMessage = "Yêu cầu hủy đơn không hợp lệ!";
            AlertType = "danger";
            return RedirectToPage(new { LookupPhone, Status });
        }

        // Kiểm tra điều kiện: Chưa tới bước giao hàng (Shipping)
        if (order.OrderStatus != "Pending" && order.OrderStatus != "Confirmed")
        {
            AlertMessage = "Đơn hàng đã được chuyển sang giao hàng hoặc hoàn thành, bạn không thể hủy nữa!";
            AlertType = "warning";
            return RedirectToPage(new { LookupPhone, Status });
        }

        // Hoàn trả tồn kho cho tất cả sản phẩm trong đơn
        foreach (var d in order.OrderDetails)
        {
            var prod = await _context.Products.FindAsync(d.ProductId);
            if (prod != null)
            {
                prod.Quantity += d.Quantity;
            }
        }

        order.OrderStatus = "Cancelled";
        await _context.SaveChangesAsync();

        AlertMessage = $"Hủy đơn hàng #ES{order.Id:D7} thành công! Số lượng sản phẩm đã được tự động hoàn lại vào kho.";
        AlertType = "success";
        return RedirectToPage(new { LookupPhone, Status });
    }
}
