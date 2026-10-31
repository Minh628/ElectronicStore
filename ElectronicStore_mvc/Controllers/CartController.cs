using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Services;

namespace ElectronicStore_mvc.Controllers;

[IgnoreAntiforgeryToken]
public class CartController : Controller
{
    private readonly ElectronicStoreDbContext _context;
    private readonly CartService _cartService;

    public CartController(ElectronicStoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    /// <summary>
    /// Hiển thị trang giỏ hàng
    /// </summary>
    public IActionResult Index()
    {
        var cart = _cartService.GetCart();
        ViewBag.TotalAmount = _cartService.GetTotal();
        ViewBag.CartCount = _cartService.GetCount();
        return View(cart);
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng qua AJAX (hỗ trợ cả handler=AddToCart)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || !product.IsActive)
        {
            return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh!" });
        }

        var result = _cartService.AddToCart(product, quantity);
        return Json(new
        {
            success = result.Success,
            message = result.Message,
            cartCount = _cartService.GetCount()
        });
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm (+/-) qua AJAX không reload trang
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        int maxStock = product?.Quantity ?? 99;

        _cartService.UpdateQuantity(productId, quantity, maxStock);

        var cart = _cartService.GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);

        return Json(new
        {
            success = true,
            cartCount = _cartService.GetCount(),
            totalAmount = _cartService.GetTotal(),
            itemQuantity = item?.Quantity ?? 0,
            itemTotal = item?.TotalPrice ?? 0,
            isRemoved = item == null
        });
    }

    /// <summary>
    /// Xóa một sản phẩm khỏi giỏ qua AJAX hoặc Form POST
    /// </summary>
    [HttpPost]
    public IActionResult Remove(int productId)
    {
        _cartService.RemoveFromCart(productId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new
            {
                success = true,
                message = "Đã xóa thiết bị khỏi giỏ hàng thành công!",
                cartCount = _cartService.GetCount(),
                totalAmount = _cartService.GetTotal(),
                isEmpty = _cartService.GetCart().Count == 0
            });
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Xóa toàn bộ giỏ hàng
    /// </summary>
    [HttpPost]
    public IActionResult Clear()
    {
        _cartService.ClearCart();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new
            {
                success = true,
                message = "Đã xóa toàn bộ giỏ hàng!",
                cartCount = 0,
                totalAmount = 0
            });
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Lấy số lượng giỏ hàng hiện tại
    /// </summary>
    [HttpGet]
    public IActionResult GetCartCount()
    {
        return Json(new { count = _cartService.GetCount() });
    }
}
