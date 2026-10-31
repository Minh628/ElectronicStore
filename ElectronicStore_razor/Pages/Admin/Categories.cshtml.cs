using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CategoriesModel : PageModel
{
    private readonly ElectronicStoreDbContext _context;

    public CategoriesModel(ElectronicStoreDbContext context)
    {
        _context = context;
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
        var query = _context.Categories
            .Include(c => c.Products)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(kw) || 
                                     (c.Description != null && c.Description.ToLower().Contains(kw)));
        }

        Categories = await query.OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(InputCategory.Name))
        {
            AlertMessage = "Tên danh mục không được để trống!";
            AlertType = "danger";
            return RedirectToPage(new { SearchKeyword });
        }

        if (InputCategory.Id == 0)
        {
            InputCategory.Name = InputCategory.Name.Trim();
            InputCategory.Description = InputCategory.Description?.Trim();
            _context.Categories.Add(InputCategory);
            AlertMessage = $"Đã thêm danh mục mới: {InputCategory.Name}";
            AlertType = "success";
        }
        else
        {
            var existing = await _context.Categories.FindAsync(InputCategory.Id);
            if (existing != null)
            {
                existing.Name = InputCategory.Name.Trim();
                existing.Description = InputCategory.Description?.Trim();
                existing.IsActive = InputCategory.IsActive;
                AlertMessage = $"Đã cập nhật danh mục: {existing.Name}";
                AlertType = "success";
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToPage(new { SearchKeyword });
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category != null)
        {
            // Đảo trạng thái hiển thị của danh mục; khi ẩn danh mục thì toàn bộ sản phẩm thuộc danh mục đó cũng tự động ẩn trên trang End-User
            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();
            var prodCount = category.Products?.Count ?? 0;
            AlertMessage = category.IsActive
                ? $"Đã hiển thị lại danh mục '{category.Name}' và các sản phẩm thuộc danh mục này trên trang bán hàng!"
                : $"Đã tạm ẩn danh mục '{category.Name}' cùng toàn bộ {prodCount} sản phẩm thuộc danh mục này khỏi trang khách hàng!";
            AlertType = "success";
        }
        return RedirectToPage(new { SearchKeyword });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category != null)
        {
            if (category.Products.Count > 0)
            {
                // Nếu danh mục đang chứa sản phẩm -> Tự động chuyển sang Tạm ẩn (Soft Delete) để ẩn toàn bộ sản phẩm liên kết trên trang End-User
                category.IsActive = false;
                await _context.SaveChangesAsync();
                AlertMessage = $"Danh mục '{category.Name}' đang có {category.Products.Count} sản phẩm liên kết nên đã được chuyển sang trạng thái TẠM ẨN (toàn bộ sản phẩm thuộc danh mục này cũng được ẩn khỏi trang khách hàng).";
                AlertType = "success";
            }
            else
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                AlertMessage = $"Đã xóa vĩnh viễn danh mục: {category.Name}";
                AlertType = "success";
            }
        }

        return RedirectToPage(new { SearchKeyword });
    }
}
