using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ElectronicStore_mvc.Models.Entities;
using ElectronicStore_mvc.Services;

namespace ElectronicStore_mvc.Controllers;

/// <summary>
/// Controller tra cứu và quản lý đơn hàng của khách hàng (Thin Controller)
/// </summary>
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
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
        var currentUser = (User.Identity != null && User.Identity.IsAuthenticated) ? await _userManager.GetUserAsync(User) : null;
        var currentUserId = currentUser?.Id;
        var userPhone = currentUser?.PhoneNumber;

        if (string.IsNullOrWhiteSpace(phoneNumber) && currentUserId == null)
        {
            ViewBag.Status = status ?? "All";
            ViewBag.AllOrders = new List<Order>();
            return View(new List<Order>());
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            ViewBag.PhoneNumber = phoneNumber.Trim();
        }

        // Gọi OrderService lấy danh sách đơn hàng bảo mật
        var allOrders = await _orderService.GetCustomerOrdersAsync(currentUserId, userPhone, phoneNumber);

        ViewBag.Status = status ?? "All";
        ViewBag.AllOrders = allOrders;

        var filtered = (status == "All" || string.IsNullOrEmpty(status))
            ? allOrders
            : allOrders.Where(o => _orderService.NormalizeStatus(o.OrderStatus) == _orderService.NormalizeStatus(status)).ToList();

        return View(filtered);
    }

    /// <summary>
    /// Khách hàng hủy đơn hàng (Bảo mật kiểm tra quyền sở hữu IDOR & hoàn kho tự động)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("my-orders/cancel")]
    [Route("Orders/CancelOrder")]
    public async Task<IActionResult> CancelOrder(int orderId, string? phoneNumber, string? status)
    {
        var currentUser = (User.Identity != null && User.Identity.IsAuthenticated) ? await _userManager.GetUserAsync(User) : null;
        var result = await _orderService.CancelCustomerOrderAsync(orderId, currentUser?.Id, currentUser?.PhoneNumber, phoneNumber);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

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
