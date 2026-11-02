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
    private readonly IAccountService _accountService;

    public CheckoutModel(
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

        // Tự động điền thông tin khách hàng từ AccountService nếu đã đăng nhập
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = await _accountService.GetCurrentUserAsync(User);
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
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = await _accountService.GetCurrentUserAsync(User);
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
