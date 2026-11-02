using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class CartModel : PageModel
{
    private readonly CartService _cartService;
    private readonly IProductService _productService;

    public CartModel(CartService cartService, IProductService productService)
    {
        _cartService = cartService;
        _productService = productService;
    }

    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalAmount { get; set; }

    public async Task OnGetAsync()
    {
        var cart = _cartService.GetCart();
        if (cart.Count > 0)
        {
            var productIds = cart.Select(c => c.ProductId).ToList();
            var dbProducts = await _productService.GetProductsByIdsAsync(productIds);
            _cartService.SyncWithProducts(dbProducts);
        }
        CartItems = _cartService.GetCart();
        TotalAmount = _cartService.GetTotal();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int productId, int quantity)
    {
        var product = await _productService.GetProductForCartAsync(productId);
        int maxStock = (product != null && product.IsActive && (product.Category == null || product.Category.IsActive)) ? product.Quantity : 0;

        _cartService.UpdateQuantity(productId, quantity, maxStock);

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
