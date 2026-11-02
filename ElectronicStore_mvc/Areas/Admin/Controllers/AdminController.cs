using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ElectronicStore_mvc.Models.Entities;
using ElectronicStore_mvc.Areas.Admin.Models;
using ElectronicStore_mvc.Services;

namespace ElectronicStore_mvc.Areas.Admin.Controllers;

/// <summary>
/// Controller trung tâm phân hệ Quản trị (Thin Controller - điều phối qua các Domain Services)
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IOrderService _orderService;
    private readonly IAccountService _accountService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public AdminController(
        IProductService productService,
        ICategoryService categoryService,
        IOrderService orderService,
        IAccountService accountService,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _productService = productService;
        _categoryService = categoryService;
        _orderService = orderService;
        _accountService = accountService;
        _userManager = userManager;
        _env = env;
    }

    public IActionResult Index() => RedirectToAction(nameof(Products));

    #region 1. QUẢN LÝ SẢN PHẨM (PRODUCTS)
    public async Task<IActionResult> Products(int? categoryId, string? search, int currentPage = 1)
    {
        var result = await _productService.GetAdminProductsAsync(categoryId, search, currentPage, pageSize: 10);
        var categories = await _categoryService.GetAllCategoriesAsync();

        ViewBag.TotalSystemProducts = result.TotalSystemProducts;
        ViewBag.InStockCount = result.InStockCount;
        ViewBag.LowOrOutCount = result.LowOrOutCount;
        ViewBag.Categories = categories.OrderBy(c => c.Name).ToList();
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.Search = search;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.CurrentPage = result.CurrentPage;

        return View(result.Products);
    }

    [HttpPost]
    public async Task<IActionResult> SaveProduct(
        Product product, IFormFile? imageFile, int? filterCategoryId, int? categoryId, string? search, int currentPage = 1)
    {
        var result = await _productService.SaveProductAsync(product, imageFile, _env.WebRootPath);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Products), new { categoryId = filterCategoryId, search, currentPage });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProduct(
        int id, int? categoryId, int? filterCategoryId, string? search, int currentPage = 1)
    {
        var result = await _productService.DeleteProductAsync(id);
        if (result.Success)
        {
            if (result.Message.Contains("TẠM ẨN"))
            {
                TempData["WarningMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Products), new { categoryId = filterCategoryId, search, currentPage });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleProductStatus(
        int id, int? categoryId, string? search, int currentPage = 1)
    {
        var result = await _productService.ToggleProductStatusAsync(id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Products), new { categoryId, search, currentPage });
    }
    #endregion

    #region 2. QUẢN LÝ DANH MỤC (CATEGORIES)
    public async Task<IActionResult> Categories(string? search)
    {
        var categories = await _categoryService.GetAllCategoriesAsync(search);
        ViewBag.SearchKeyword = search;
        return View(categories);
    }

    [HttpPost]
    public async Task<IActionResult> SaveCategory(Category category, string? search)
    {
        var result = await _categoryService.SaveCategoryAsync(category);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Categories), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleCategoryStatus(int id, string? search)
    {
        var result = await _categoryService.ToggleCategoryStatusAsync(id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Categories), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, string? search)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Categories), new { search });
    }
    #endregion

    #region 3. QUẢN LÝ ĐƠN HÀNG (ORDERS)
    public static string NormalizeStatus(string? status) => OrderService.NormalizeStatusStatic(status);

    public async Task<IActionResult> Orders(string? status, string? search)
    {
        var result = await _orderService.GetAdminOrdersAsync(status, search);

        ViewBag.TotalOrdersCount = result.TotalOrdersCount;
        ViewBag.PendingCount = result.PendingCount;
        ViewBag.ShippingCount = result.ShippingCount;
        ViewBag.CompletedCount = result.CompletedCount;
        ViewBag.TotalRevenue = result.TotalRevenue;
        ViewBag.CurrentStatus = status ?? "All";
        ViewBag.SearchKeyword = search;

        return View(result.Orders);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, string status, string? currentStatus, string? search)
    {
        var result = await _orderService.UpdateOrderStatusAsync(orderId, status);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Orders), new { status = currentStatus, search });
    }
    #endregion

    #region 4. QUẢN LÝ TÀI KHOẢN (ACCOUNTS)
    public async Task<IActionResult> Accounts(string? search)
    {
        var dtos = await _accountService.GetAdminAccountsAsync(search);
        var list = dtos.Select(d => new UserAccountViewModel
        {
            User = d.User,
            Role = d.Role
        }).ToList();

        ViewBag.SearchKeyword = search;
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount(
        string username, string fullName, string email, string phone, string address, string password, string role, string? search)
    {
        var result = await _accountService.CreateAccountAsync(username, fullName, email, phone, address, password, role);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Accounts), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAccount(
        string userId, string fullName, string email, string phone, string address, string role, string? search)
    {
        var currentAdminId = _userManager.GetUserId(User) ?? string.Empty;
        var result = await _accountService.UpdateAccountAsync(currentAdminId, userId, fullName, email, phone, address, role);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Accounts), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAccount(string userId, string? search)
    {
        var currentAdminId = _userManager.GetUserId(User) ?? string.Empty;
        var result = await _accountService.DeleteAccountAsync(currentAdminId, userId);

        if (result.Success)
        {
            if (result.Message.Contains("KHÓA VĨNH VIỄN"))
            {
                TempData["WarningMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Accounts), new { search });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAccountLock(string userId, string? search)
    {
        var currentAdminId = _userManager.GetUserId(User) ?? string.Empty;
        var result = await _accountService.ToggleAccountLockAsync(currentAdminId, userId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Accounts), new { search });
    }
    #endregion
}
