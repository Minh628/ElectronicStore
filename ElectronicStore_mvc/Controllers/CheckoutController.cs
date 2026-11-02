using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ElectronicStore_mvc.Models.Entities;
using ElectronicStore_mvc.Services;

namespace ElectronicStore_mvc.Controllers;

/// <summary>
/// Controller điều phối quy trình đặt hàng và thanh toán (Thin Controller)
/// </summary>
public class CheckoutController : Controller
{
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly CartService _cartService;
    private readonly IAccountService _accountService;

    public CheckoutController(
        IProductService productService,
        IOrderService orderService,
        CartService cartService,
        IAccountService accountService)
    {
        _productService = productService;
        _orderService = orderService;
        _cartService = cartService;
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = _cartService.GetCart();
        if (cart.Any())
        {
            var productIds = cart.Select(c => c.ProductId).ToList();
            var dbProducts = await _productService.GetProductsByIdsAsync(productIds);
            _cartService.SyncWithProducts(dbProducts);
            cart = _cartService.GetCart();
        }
        if (!cart.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        ViewBag.CartItems = cart;
        ViewBag.TotalAmount = _cartService.GetTotal();
        bool isAuthenticated = User.Identity?.IsAuthenticated == true;
        ViewBag.IsAuthenticated = isAuthenticated;

        // Nếu đã đăng nhập thì tự động điền thông tin người dùng từ Identity ApplicationUser
        if (isAuthenticated)
        {
            var user = await _accountService.GetCurrentUserAsync(User);
            if (user != null)
            {
                ViewBag.FullName = user.FullName ?? user.UserName;
                ViewBag.PhoneNumber = user.PhoneNumber;
                ViewBag.Email = user.Email;
                ViewBag.ShippingAddress = user.Address == "Chưa cập nhật địa chỉ" ? "" : user.Address;
            }
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Process(
        string fullName, string phoneNumber, string? email, string shippingAddress, string paymentMethod, string? notes)
    {
        var cart = _cartService.GetCart();
        if (!cart.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        string? currentUserId = null;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = await _accountService.GetCurrentUserAsync(User);
            currentUserId = user?.Id;
        }

        // Gọi OrderService thực hiện toàn bộ nghiệp vụ kiểm tra tồn kho, danh mục, giao dịch Serializable và trừ kho
        var result = await _orderService.PlaceOrderAsync(
            fullName, phoneNumber, email, shippingAddress, paymentMethod, notes, cart, currentUserId);

        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            ViewBag.CartItems = cart;
            ViewBag.TotalAmount = _cartService.GetTotal();
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;
            ViewBag.FullName = fullName;
            ViewBag.PhoneNumber = phoneNumber;
            ViewBag.Email = email;
            ViewBag.ShippingAddress = shippingAddress;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.Notes = notes;
            return View("Index");
        }

        // Xóa giỏ hàng và lưu session cho khách vãng lai
        HttpContext.Session.SetInt32("LastGuestOrderId", result.OrderId);
        _cartService.ClearCart();

        return RedirectToAction("OrderSuccess", new { id = result.OrderId });
    }

    [HttpGet]
    public async Task<IActionResult> OrderSuccess(int? id, int? orderId, string? phone)
    {
        int actualId = id ?? orderId ?? HttpContext.Session.GetInt32("LastGuestOrderId") ?? 0;
        var user = (User.Identity != null && User.Identity.IsAuthenticated) ? await _accountService.GetCurrentUserAsync(User) : null;
        bool isAdmin = User.IsInRole("Admin");
        int? lastGuestId = HttpContext.Session.GetInt32("LastGuestOrderId");

        // Gọi OrderService kiểm tra quyền sở hữu đơn hàng bảo mật IDOR
        var order = await _orderService.GetOrderForViewerAsync(
            actualId, user?.Id, user?.PhoneNumber, isAdmin, lastGuestId, phone);

        return View(order);
    }
}
