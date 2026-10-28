using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Controllers;

public class OrdersController : Controller
{
    private readonly ElectronicStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(ElectronicStoreDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Hiển thị danh sách đơn hàng của khách hàng (theo tài khoản hoặc số điện thoại)
    /// </summary>
    [HttpGet]
    [Route("my-orders")]
    [Route("Orders/MyOrders")]
    public async Task<IActionResult> MyOrders(string? phoneNumber, string? status = "All")
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

        // 1. Nếu đã đăng nhập, lọc theo UserId của tài khoản Identity
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                query = query.Where(o => o.UserId == user.Id);
            }
        }
        else if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            // 2. Nếu khách tra cứu nhanh bằng SĐT nhận hàng
            query = query.Where(o => o.ReceiverPhone == phoneNumber.Trim());
            ViewBag.PhoneNumber = phoneNumber.Trim();
        }
        else
        {
            ViewBag.Status = status ?? "All";
            ViewBag.AllOrders = new List<Order>();
            return View(new List<Order>());
        }

        var allOrders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        ViewBag.Status = status ?? "All";
        ViewBag.AllOrders = allOrders;

        var filtered = (status == "All" || string.IsNullOrEmpty(status))
            ? allOrders
            : allOrders.Where(o => o.OrderStatus == status).ToList();

        return View(filtered);
    }

    /// <summary>
    /// Khách hàng hủy đơn hàng (Bảo mật kiểm tra quyền sở hữu IDOR & chỉ hủy khi Pending/Confirmed)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("my-orders/cancel")]
    [Route("Orders/CancelOrder")]
    public async Task<IActionResult> CancelOrder(int orderId, string? phoneNumber, string? status)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần hủy.";
            return RedirectToAction(nameof(MyOrders), new { phoneNumber, status });
        }

        // 1. Bảo mật chống tấn công IDOR: Kiểm tra quyền sở hữu đơn hàng
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || order.UserId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thao tác trên đơn hàng này!";
                return RedirectToAction(nameof(MyOrders), new { phoneNumber, status });
            }
        }
        else if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            if (order.ReceiverPhone != phoneNumber.Trim())
            {
                TempData["ErrorMessage"] = "Số điện thoại tra cứu không khớp với đơn hàng!";
                return RedirectToAction(nameof(MyOrders), new { phoneNumber, status });
            }

            // Nếu đơn này thuộc về tài khoản đã đăng ký thành viên nhưng đang tra cứu ẩn danh: yêu cầu đăng nhập
            if (!string.IsNullOrEmpty(order.UserId))
            {
                TempData["ErrorMessage"] = "Đơn hàng này thuộc tài khoản thành viên. Vui lòng đăng nhập tài khoản để hủy đơn!";
                return RedirectToAction(nameof(MyOrders), new { phoneNumber, status });
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Yêu cầu không hợp lệ!";
            return RedirectToAction(nameof(MyOrders), new { phoneNumber, status });
        }

        // 2. Kiểm tra điều kiện: chỉ hủy khi đơn chưa được chuyển sang Shipping hoặc Completed
        if (order.OrderStatus != "Pending" && order.OrderStatus != "Confirmed")
        {
            TempData["ErrorMessage"] = "Đơn hàng đang giao hoặc đã hoàn tất/đã hủy, không thể hủy.";
            return RedirectToAction(nameof(MyOrders), new { phoneNumber, status });
        }

        // 3. Hoàn lại số lượng tồn kho cho từng sản phẩm trong đơn
        foreach (var detail in order.OrderDetails)
        {
            var product = await _context.Products.FindAsync(detail.ProductId);
            if (product != null)
            {
                product.Quantity += detail.Quantity;
            }
        }

        order.OrderStatus = "Cancelled";
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã hủy thành công đơn hàng #ES{order.Id:D7}. Số lượng sản phẩm đã được tự động hoàn lại vào kho.";
        return RedirectToAction(nameof(MyOrders), new { phoneNumber, status });
    }

    /// <summary>
    /// Xem chi tiết một đơn hàng (Điều hướng an toàn sang giao diện Hóa đơn OrderSuccess)
    /// </summary>
    [HttpGet]
    [Route("Orders/Details/{id}")]
    [Route("order-details/{id}")]
    public IActionResult Details(int id)
    {
        return RedirectToAction("OrderSuccess", "Checkout", new { id });
    }
}
