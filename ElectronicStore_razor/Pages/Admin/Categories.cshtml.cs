using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services.Admin;

namespace ElectronicStore_razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CategoriesModel : PageModel
{
    private readonly IAdminCategoryService _categoryService;

    public CategoriesModel(IAdminCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public List<Category> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [BindProperty]
    public Category InputCategory { get; set; } = new();

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _categoryService.GetAllCategoriesAsync(SearchKeyword);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var result = await _categoryService.SaveCategoryAsync(InputCategory);
        AlertMessage = result.Message;
        AlertType = result.Success ? "success" : "danger";
        return RedirectToPage(new { SearchKeyword });
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var result = await _categoryService.ToggleCategoryStatusAsync(id);
        AlertMessage = result.Message;
        AlertType = result.Success ? "success" : "danger";
        return RedirectToPage(new { SearchKeyword });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);
        AlertMessage = result.Message;
        AlertType = result.Success ? "success" : "danger";
        return RedirectToPage(new { SearchKeyword });
    }
}
