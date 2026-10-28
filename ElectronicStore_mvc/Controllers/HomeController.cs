using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models;
using ElectronicStore_mvc.Models.Entities;
using ElectronicStore_mvc.Services;

namespace ElectronicStore_mvc.Controllers;

public class HomeController : Controller
{
    private readonly ElectronicStoreDbContext _context;
    private readonly CartService _cartService;

    public HomeController(ElectronicStoreDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    /// <summary>
    /// Trang chủ hiển thị danh sách thiết bị điện tử, lọc theo danh mục, hãng, tìm kiếm, sắp xếp và phân trang
    /// </summary>
    public async Task<IActionResult> Index(int? CategoryId, int? category, string? Brand, string? SearchKeyword, string? q, string? SortBy = "featured", int CurrentPage = 1)
    {
        CategoryId ??= category;
        SearchKeyword = !string.IsNullOrWhiteSpace(SearchKeyword) ? SearchKeyword : q;
        SortBy = string.IsNullOrWhiteSpace(SortBy) ? "featured" : SortBy;

        // 1. Tải danh mục và hãng để làm bộ lọc
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToListAsync();

        var brands = await _context.Products
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

        int pageSize = 8;
        int totalProducts = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > totalPages && totalPages > 0) CurrentPage = totalPages;

        // Sắp xếp theo yêu cầu
        query = SortBy switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.Id)
        };

        var products = await query
            .Skip((CurrentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.Brands = brands;
        ViewBag.CategoryId = CategoryId;
        ViewBag.Brand = Brand;
        ViewBag.SearchKeyword = SearchKeyword;
        ViewBag.SortBy = SortBy;
        ViewBag.CurrentPage = CurrentPage;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;

        return View(products);
    }

    /// <summary>
    /// Trang chi tiết sản phẩm hiển thị thông số kỹ thuật, giá ưu đãi và các sản phẩm cùng danh mục
    /// </summary>
    public async Task<IActionResult> ProductDetail(int? id)
    {
        ElectronicStore_mvc.Models.Entities.Product? product = null;

        // Nếu có id hợp lệ thì tìm theo id, ngược lại fallback lấy sản phẩm đầu tiên giống Blazor
        if (id.HasValue && id.Value > 0)
        {
            product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id.Value && p.IsActive);
        }
        else
        {
            product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.IsActive);
        }

        if (product == null)
        {
            return View(null);
        }

        // 4 sản phẩm gợi ý cùng danh mục
        var relatedProducts = await _context.Products
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
            .Take(4)
            .ToListAsync();

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
