using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class ProductDetailModel : PageModel
{
    private readonly ElectronicStoreDbContext _context;
    private readonly CartService _cartService;

    public ProductDetailModel(ElectronicStoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public Product? Product { get; set; }
    public List<Product> RelatedProducts { get; set; } = new();

    [BindProperty]
    public int Quantity { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id.HasValue && id.Value > 0)
        {
            Product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id.Value && p.IsActive);
        }
        else
        {
            Product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.IsActive);
        }

        if (Product != null)
        {
            // Tải các sản phẩm liên quan cùng danh mục
            RelatedProducts = await _context.Products
                .Where(p => p.CategoryId == Product.CategoryId && p.Id != Product.Id && p.IsActive)
                .Take(4)
                .ToListAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int id, int quantity = 1)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null || !product.IsActive)
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
        var product = await _context.Products.FindAsync(productId);
        if (product == null || !product.IsActive)
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
