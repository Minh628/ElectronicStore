using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class CartModel : PageModel
{
    private readonly CartService _cartService;
    private readonly ElectronicStoreDbContext _context;

    public CartModel(CartService cartService, ElectronicStoreDbContext context)
    {
        _cartService = cartService;
        _context = context;
    }

    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalAmount { get; set; }

    public void OnGet()
    {
        CartItems = _cartService.GetCart();
        TotalAmount = _cartService.GetTotal();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int productId, int quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        int maxStock = product?.Quantity ?? 99;

        // Cập nhật số lượng trong session giỏ hàng
        _cartService.UpdateQuantity(productId, quantity, maxStock);

        // Nếu gọi từ AJAX fetch API: trả về JSON để cập nhật giao diện mượt mà không F5
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var cart = _cartService.GetCart();
            var updatedItem = cart.FirstOrDefault(c => c.ProductId == productId);
            return new JsonResult(new
            {
                success = true,
                cartCount = _cartService.GetCount(),
                totalAmount = _cartService.GetTotal(),
                itemQuantity = updatedItem?.Quantity ?? 0,
                itemTotal = updatedItem?.TotalPrice ?? 0,
                isRemoved = updatedItem == null
            });
        }

        return RedirectToPage();
    }

    public IActionResult OnPostRemove(int productId)
    {
        _cartService.RemoveFromCart(productId);

        // Trả về JSON cho AJAX khi xóa sản phẩm
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return new JsonResult(new
            {
                success = true,
                cartCount = _cartService.GetCount(),
                totalAmount = _cartService.GetTotal(),
                isEmpty = _cartService.GetCart().Count == 0
            });
        }

        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        _cartService.ClearCart();
        return RedirectToPage();
    }
}
