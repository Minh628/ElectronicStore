using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class IndexModel : PageModel
{
    private readonly ElectronicStoreDbContext _context;
    private readonly CartService _cartService;

    public IndexModel(ElectronicStoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<string> Brands { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Brand { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; } = "featured";

    // Phân trang
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 8;
    public int TotalPages { get; set; }
    public int TotalProducts { get; set; }

    public async Task OnGetAsync()
    {
        CategoryId ??= category;
        SearchKeyword = !string.IsNullOrWhiteSpace(SearchKeyword) ? SearchKeyword : q;
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? "featured" : SortBy;

        // Tải danh mục và hãng để làm bộ lọc
        Categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToListAsync();

        Brands = await _context.Products
            .Where(p => p.IsActive)
            .Select(p => p.Brand)
            .Distinct()
            .ToListAsync();

        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        // Lọc theo danh mục
        if (CategoryId.HasValue && CategoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == CategoryId.Value);
        }

        // Lọc theo hãng sản xuất
        if (!string.IsNullOrWhiteSpace(Brand))
        {
            query = query.Where(p => p.Brand == Brand);
        }

        // Tìm kiếm theo tên sản phẩm hoặc thông số
        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw) || 
                                     (p.Specifications != null && p.Specifications.ToLower().Contains(kw)));
        }

        TotalProducts = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalProducts / (double)PageSize);
        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

        // Sắp xếp
        query = SortBy switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.Id)
        };

        Products = await query
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Handler xử lý AJAX thêm nhanh sản phẩm vào giỏ hàng
    /// </summary>
    public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity = 1)
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
