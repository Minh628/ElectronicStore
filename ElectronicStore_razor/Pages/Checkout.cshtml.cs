using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class CheckoutModel : PageModel
{
    private readonly ElectronicStoreDbContext _context;
    private readonly CartService _cartService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutModel(
        ElectronicStoreDbContext context, 
        CartService cartService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _cartService = cartService;
        _userManager = userManager;
    }

    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalAmount { get; set; }

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string PhoneNumber { get; set; } = string.Empty;

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string ShippingAddress { get; set; } = string.Empty;

    [BindProperty]
    public string PaymentMethod { get; set; } = "COD";

    [BindProperty]
    public string? Notes { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CartItems = _cartService.GetCart();
        if (CartItems.Count == 0)
        {
            return RedirectToPage("/Cart");
        }

        TotalAmount = _cartService.GetTotal();

        // Tự động điền thông tin khách hàng từ ApplicationUser nếu đã đăng nhập
        var username = User.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user != null)
            {
                // Fallback về UserName nếu chưa cập nhật FullName giống MVC và Blazor
                FullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.UserName ?? string.Empty);
                PhoneNumber = user.PhoneNumber ?? string.Empty;
                Email = user.Email;
                ShippingAddress = user.Address == "Chưa cập nhật địa chỉ" ? string.Empty : (user.Address ?? string.Empty);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CartItems = _cartService.GetCart();
        if (CartItems.Count == 0)
        {
            return RedirectToPage("/Cart");
        }

        TotalAmount = _cartService.GetTotal();

        if (PaymentMethod is not ("COD" or "BankTransfer" or "VNPAY" or "MOMO"))
        {
            ErrorMessage = "Phương thức thanh toán không hợp lệ.";
            return Page();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(PhoneNumber) || string.IsNullOrWhiteSpace(ShippingAddress) || ShippingAddress.Trim() == "Chưa cập nhật địa chỉ")
        {
            ErrorMessage = "Vui lòng điền đầy đủ Họ tên, Số điện thoại và Địa chỉ nhận hàng!";
            return Page();
        }

        // 1. Kiểm tra tính khả dụng và tồn kho thực tế của từng sản phẩm (Chống bán khống)
        foreach (var item in CartItems)
        {
            var prod = await _context.Products.FindAsync(item.ProductId);
            if (prod == null || !prod.IsActive)
            {
                ErrorMessage = $"Sản phẩm '{item.ProductName}' hiện không còn kinh doanh!";
                return Page();
            }
            if (prod.Quantity < item.Quantity)
            {
                ErrorMessage = $"Sản phẩm '{prod.Name}' chỉ còn {prod.Quantity} chiếc trong kho (bạn đặt {item.Quantity} chiếc)!";
                return Page();
            }

            item.ProductName = prod.Name;
            item.UnitPrice = prod.Price;
        }

        // 2. Xác định tài khoản đặt hàng (nếu đã đăng nhập thì liên kết UserId và cập nhật Profile)
        string? currentUserId = null;
        var username = User.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user != null)
            {
                currentUserId = user.Id;

                // Cập nhật thông tin bổ sung nếu user chưa có
                bool userUpdated = false;
                if (string.IsNullOrEmpty(user.FullName))
                {
                    user.FullName = FullName.Trim();
                    userUpdated = true;
                }
                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    user.PhoneNumber = PhoneNumber.Trim();
                    userUpdated = true;
                }
                if (string.IsNullOrEmpty(user.Address) || user.Address == "Chưa cập nhật địa chỉ")
                {
                    user.Address = ShippingAddress.Trim();
                    userUpdated = true;
                }
                if (userUpdated)
                {
                    await _userManager.UpdateAsync(user);
                }
            }
        }

        // 3. Tạo Hóa đơn (Order) gắn với UserId (null nếu là khách vãng lai) và lưu thông tin người nhận
        var order = new Order
        {
            UserId = currentUserId,
            ReceiverName = FullName.Trim(),
            ReceiverPhone = PhoneNumber.Trim(),
            OrderDate = DateTime.Now,
            TotalAmount = CartItems.Sum(item => item.TotalPrice),
            OrderStatus = "Pending",
            PaymentMethod = PaymentMethod,
            ShippingAddress = ShippingAddress.Trim(),
            Notes = Notes?.Trim()
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // 5. Tạo các Chi tiết hóa đơn (OrderDetails) & Trừ tồn kho
        foreach (var item in CartItems)
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

            // Cập nhật trừ số lượng tồn kho của sản phẩm
            var prod = await _context.Products.FindAsync(item.ProductId);
            if (prod != null)
            {
                prod.Quantity = Math.Max(0, prod.Quantity - item.Quantity);
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // 6. Xóa sạch giỏ hàng trong Session
        HttpContext.Session.SetInt32("LastGuestOrderId", order.Id);
        _cartService.ClearCart();

        return RedirectToPage("/OrderSuccess", new { orderId = order.Id });
    }
}
