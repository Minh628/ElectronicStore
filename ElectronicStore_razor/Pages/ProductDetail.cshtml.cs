using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class ProductDetailModel : PageModel
{
    private readonly IProductService _productService;
    private readonly CartService _cartService;

    public ProductDetailModel(IProductService productService, CartService cartService)
    {
        _productService = productService;
        _cartService = cartService;
    }

    public Product? Product { get; set; }
    public List<Product> RelatedProducts { get; set; } = new();

    [BindProperty]
    public int Quantity { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Product = await _productService.GetProductDetailAsync(id);

        if (Product != null)
        {
            RelatedProducts = await _productService.GetRelatedProductsAsync(Product.CategoryId, Product.Id, count: 4);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int id, int quantity = 1)
    {
        var product = await _productService.GetProductForCartAsync(id);
        if (product == null || !product.IsActive || (product.Category != null && !product.Category.IsActive))
        {
            return RedirectToPage("/Index");
        }

        _cartService.AddToCart(product, quantity);
        return RedirectToPage("/Cart");
    }

    /// <summary>
    /// Handler xử lý thêm vào giỏ qua AJAX trên trang chi tiết sản phẩm
    /// </summary>
    public async Task<IActionResult> OnPostAddToCartAjaxAsync(int productId, int quantity = 1)
    {
        var product = await _productService.GetProductForCartAsync(productId);
        if (product == null || !product.IsActive || (product.Category != null && !product.Category.IsActive))
        {
            return new JsonResult(new { success = false, message = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh!" });
        }

        var result = _cartService.AddToCart(product, quantity);
        return new JsonResult(new
        {
            success = result.Success,
            message = result.Message,
            cartCount = _cartService.GetCount(),
            count = _cartService.GetCount()
        });
    }
}
