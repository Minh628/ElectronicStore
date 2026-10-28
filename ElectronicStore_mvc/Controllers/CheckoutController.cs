using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;
using ElectronicStore_mvc.Services;

namespace ElectronicStore_mvc.Controllers;

public class CheckoutController : Controller
{
    private readonly ElectronicStoreDbContext _context;
    private readonly CartService _cartService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutController(
        ElectronicStoreDbContext context, 
        CartService cartService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _cartService = cartService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = _cartService.GetCart();
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
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.FullName = user.FullName ?? user.UserName;
                ViewBag.PhoneNumber = user.PhoneNumber;
                ViewBag.Email = user.Email;
                ViewBag.ShippingAddress = user.Address;
            }
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Process(string fullName, string phoneNumber, string? email, string shippingAddress, string paymentMethod, string? notes)
    {
        var cart = _cartService.GetCart();
        if (!cart.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        // Hàm cục bộ điền lại dữ liệu form khi validation lỗi để người dùng không phải gõ lại
        void PreserveFormData()
        {
            ViewBag.CartItems = cart;
            ViewBag.TotalAmount = _cartService.GetTotal();
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;
            ViewBag.FullName = fullName;
            ViewBag.PhoneNumber = phoneNumber;
            ViewBag.Email = email;
            ViewBag.ShippingAddress = shippingAddress;
            ViewBag.Notes = notes;
        }

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(shippingAddress))
        {
            ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ Họ tên, Số điện thoại và Địa chỉ giao hàng!";
            PreserveFormData();
            return View("Index");
        }

        // 1. Kiểm tra tính khả dụng và tồn kho thực tế của từng sản phẩm (Chống bán khống)
        foreach (var item in cart)
        {
            var prod = await _context.Products.FindAsync(item.ProductId);
            if (prod == null || !prod.IsActive)
            {
                ViewBag.ErrorMessage = $"Sản phẩm '{item.ProductName}' hiện không còn kinh doanh!";
                PreserveFormData();
                return View("Index");
            }
            if (prod.Quantity < item.Quantity)
            {
                ViewBag.ErrorMessage = $"Sản phẩm '{prod.Name}' chỉ còn {prod.Quantity} chiếc trong kho (bạn đặt {item.Quantity} chiếc)!";
                PreserveFormData();
                return View("Index");
            }
        }

        // 2. Xác định tài khoản đặt hàng (nếu đã đăng nhập thì liên kết UserId và cập nhật Profile)
        string? currentUserId = null;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                currentUserId = user.Id;

                // Tự động cập nhật thông tin bổ sung nếu tài khoản chưa có
                bool userUpdated = false;
                if (string.IsNullOrEmpty(user.FullName))
                {
                    user.FullName = fullName.Trim();
                    userUpdated = true;
                }
                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    user.PhoneNumber = phoneNumber.Trim();
                    userUpdated = true;
                }
                if (string.IsNullOrEmpty(user.Address))
                {
                    user.Address = shippingAddress.Trim();
                    userUpdated = true;
                }
                if (userUpdated)
                {
                    await _userManager.UpdateAsync(user);
                }
            }
        }

        // 3. Tạo hóa đơn Order (UserId có thể là null cho khách vãng lai hoặc Id của thành viên)
        var order = new Order
        {
            UserId = currentUserId,
            OrderDate = DateTime.Now,
            TotalAmount = _cartService.GetTotal(),
            OrderStatus = "Pending",
            PaymentMethod = paymentMethod ?? "COD",
            ReceiverName = fullName.Trim(),
            ReceiverPhone = phoneNumber.Trim(),
            ShippingAddress = shippingAddress.Trim(),
            Notes = notes?.Trim()
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // 5. Thêm chi tiết đơn hàng OrderDetails và trừ kho sản phẩm
        foreach (var item in cart)
        {
            var orderDetail = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            };
            _context.OrderDetails.Add(orderDetail);

            // Trừ số lượng tồn kho sản phẩm trong database
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.Quantity = Math.Max(0, product.Quantity - item.Quantity);
            }
        }

        await _context.SaveChangesAsync();

        // 6. Xóa sạch giỏ hàng trong session sau khi đặt hàng thành công
        _cartService.ClearCart();

        return RedirectToAction("OrderSuccess", new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> OrderSuccess(int? id, int? orderId)
    {
        int actualId = id ?? orderId ?? 0;
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == actualId);

        return View(order);
    }
}
