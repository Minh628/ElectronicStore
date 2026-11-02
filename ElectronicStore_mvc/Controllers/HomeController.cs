using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ElectronicStore_mvc.Models;
using ElectronicStore_mvc.Services;

namespace ElectronicStore_mvc.Controllers;

/// <summary>
/// Controller trang chủ và xem chi tiết thiết bị điện tử (Thin Controller)
/// </summary>
public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly CartService _cartService;

    public HomeController(
        IProductService productService,
        ICategoryService categoryService,
        CartService cartService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _cartService = cartService;
    }

    /// <summary>
    /// Trang chủ hiển thị danh sách thiết bị điện tử, lọc theo danh mục, hãng, tìm kiếm, sắp xếp và phân trang
    /// </summary>
    public async Task<IActionResult> Index(
        int? CategoryId, int? category, string? Brand, string? SearchKeyword, string? q, string? SortBy = "featured", int CurrentPage = 1)
    {
        CategoryId ??= category;
        SearchKeyword = !string.IsNullOrWhiteSpace(SearchKeyword) ? SearchKeyword : q;
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? "featured" : SortBy;

        // 1. Tải danh mục và thương hiệu đang hoạt động từ tầng Service
        var categories = await _categoryService.GetActiveCategoriesAsync();
        var brands = await _productService.GetActiveBrandsAsync();

        // 2. Lấy danh sách sản phẩm thỏa mãn điều kiện lọc qua Service
        var catalogResult = await _productService.GetCatalogProductsAsync(
            CategoryId, Brand, SearchKeyword, SortBy, CurrentPage, pageSize: 8);

        ViewBag.Categories = categories;
        ViewBag.Brands = brands;
        ViewBag.CategoryId = CategoryId;
        ViewBag.Brand = Brand;
        ViewBag.SearchKeyword = SearchKeyword;
        ViewBag.SortBy = SortBy;
        ViewBag.CurrentPage = catalogResult.CurrentPage;
        ViewBag.TotalPages = catalogResult.TotalPages;
        ViewBag.TotalProducts = catalogResult.TotalProducts;

        return View(catalogResult.Products);
    }

    /// <summary>
    /// Trang chi tiết sản phẩm hiển thị thông số kỹ thuật, giá ưu đãi và các sản phẩm cùng danh mục
    /// </summary>
    public async Task<IActionResult> ProductDetail(int? id)
    {
        var product = await _productService.GetProductDetailAsync(id);
        if (product == null)
        {
            return View(null);
        }

        // Lấy 4 sản phẩm gợi ý cùng danh mục từ Service
        var relatedProducts = await _productService.GetRelatedProductsAsync(product.CategoryId, product.Id, count: 4);
        ViewBag.RelatedProducts = relatedProducts;

        return View(product);
    }

    /// <summary>
    /// Điều hướng từ route cũ Details sang ProductDetail để đảm bảo tương thích
    /// </summary>
    [HttpGet("Home/Details/{id?}")]
    [HttpGet("Details/{id?}")]
    public IActionResult Details(int? id)
    {
        return RedirectToAction(nameof(ProductDetail), new { id });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
