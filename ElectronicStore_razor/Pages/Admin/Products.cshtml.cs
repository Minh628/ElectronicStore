using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services.Admin;

namespace ElectronicStore_razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ProductsModel : PageModel
{
    private readonly IAdminProductService _productService;
    private readonly IAdminCategoryService _categoryService;
    private readonly IWebHostEnvironment _env;

    public ProductsModel(
        IAdminProductService productService,
        IAdminCategoryService categoryService,
        IWebHostEnvironment env)
    {
        _productService = productService;
        _categoryService = categoryService;
        _env = env;
    }

    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BrandFilter { get; set; }

    // Phân trang
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    // KPI thống kê toàn hệ thống
    public int TotalSystemProducts { get; set; }
    public int InStockCount { get; set; }
    public int LowOrOutCount { get; set; }

    // Model binding cho Form Thêm / Sửa
    [BindProperty]
    public Product InputProduct { get; set; } = new();

    // Hỗ trợ tải ảnh từ máy tính (Desktop)
    [BindProperty]
    public IFormFile? ImageUpload { get; set; }

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public async Task OnGetAsync()
    {
        Categories = (await _categoryService.GetAllCategoriesAsync()).OrderBy(c => c.Name).ToList();

        var result = await _productService.GetAdminProductsAsync(
            CategoryId, BrandFilter, SearchKeyword, CurrentPage, PageSize);

        TotalSystemProducts = result.TotalSystemProducts;
        InStockCount = result.InStockCount;
        LowOrOutCount = result.LowOrOutCount;
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
        CurrentPage = result.CurrentPage;
        Products = result.Products;
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var result = await _productService.SaveProductAsync(InputProduct, ImageUpload, _env.WebRootPath);
        AlertMessage = result.Message;
        AlertType = result.Success ? "success" : "danger";
        return RedirectToPage(new { SearchKeyword, CategoryId, CurrentPage });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result = await _productService.DeleteProductAsync(id);
        AlertMessage = result.Message;
        AlertType = result.Message.Contains("TẠM ẨN") ? "warning" : (result.Success ? "success" : "danger");
        return RedirectToPage(new { SearchKeyword, CategoryId, CurrentPage });
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var result = await _productService.ToggleProductStatusAsync(id);
        AlertMessage = result.Message;
        AlertType = result.Success ? "info" : "danger";
        return RedirectToPage(new { SearchKeyword, CategoryId, CurrentPage });
    }
}
