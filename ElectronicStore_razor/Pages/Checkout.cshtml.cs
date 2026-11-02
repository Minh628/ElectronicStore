using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class CheckoutModel : PageModel
{
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly CartService _cartService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutModel(
        IProductService productService,
        IOrderService orderService,
        CartService cartService,
        UserManager<ApplicationUser> userManager)
    {
        _productService = productService;
        _orderService = orderService;
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
        if (CartItems.Count > 0)
        {
            var productIds = CartItems.Select(c => c.ProductId).ToList();
            var dbProducts = await _productService.GetProductsByIdsAsync(productIds);
            _cartService.SyncWithProducts(dbProducts);
            CartItems = _cartService.GetCart();
        }
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

        string? currentUserId = null;
        var username = User.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            var user = await _userManager.FindByNameAsync(username);
            currentUserId = user?.Id;
        }

        var result = await _orderService.PlaceOrderAsync(
            FullName, PhoneNumber, Email, ShippingAddress, PaymentMethod, Notes, CartItems, currentUserId);

        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        HttpContext.Session.SetInt32("LastGuestOrderId", result.OrderId);
        _cartService.ClearCart();

        return RedirectToPage("/OrderSuccess", new { orderId = result.OrderId });
    }
}
