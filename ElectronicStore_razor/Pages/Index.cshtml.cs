using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages;

public class IndexModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly CartService _cartService;

    public IndexModel(
        IProductService productService,
        ICategoryService categoryService,
        CartService cartService)
    {
        _productService = productService;
        _categoryService = categoryService;
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

        // Tải danh mục và hãng qua Service
        Categories = await _categoryService.GetActiveCategoriesAsync();
        Brands = await _productService.GetActiveBrandsAsync();

        var result = await _productService.GetCatalogProductsAsync(
            CategoryId, Brand, SearchKeyword, SortBy, CurrentPage, PageSize);

        TotalProducts = result.TotalProducts;
        TotalPages = result.TotalPages;
        CurrentPage = result.CurrentPage;
        Products = result.Products;
    }

    /// <summary>
    /// Handler xử lý AJAX thêm nhanh sản phẩm vào giỏ hàng
    /// </summary>
    public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity = 1)
    {
        var product = await _productService.GetProductDetailAsync(productId);
        if (product == null)
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
