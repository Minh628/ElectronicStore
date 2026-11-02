using Microsoft.AspNetCore.Mvc;
using ElectronicStore_mvc.Services;

namespace ElectronicStore_mvc.Controllers;

/// <summary>
/// Controller xử lý thao tác giỏ hàng (Thin Controller)
/// </summary>
[IgnoreAntiforgeryToken]
public class CartController : Controller
{
    private readonly IProductService _productService;
    private readonly CartService _cartService;

    public CartController(IProductService productService, CartService cartService)
    {
        _productService = productService;
        _cartService = cartService;
    }

    /// <summary>
    /// Hiển thị trang giỏ hàng và đồng bộ tồn kho/đơn giá thực tế từ Database thông qua Service
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var cart = _cartService.GetCart();
        if (cart.Count > 0)
        {
            var productIds = cart.Select(c => c.ProductId).ToList();
            var dbProducts = await _productService.GetProductsByIdsAsync(productIds);
            _cartService.SyncWithProducts(dbProducts);
            cart = _cartService.GetCart();
        }
        ViewBag.TotalAmount = _cartService.GetTotal();
        ViewBag.CartCount = _cartService.GetCount();
        return View(cart);
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng qua AJAX
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var product = await _productService.GetProductForCartAsync(productId);
        if (product == null || !product.IsActive || (product.Category != null && !product.Category.IsActive))
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
        var product = await _productService.GetProductForCartAsync(productId);
        int maxStock = (product != null && product.IsActive && (product.Category == null || product.Category.IsActive)) ? product.Quantity : 0;

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
