using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Areas.Admin.Services;

/// <summary>
/// Triển khai nghiệp vụ quản trị danh mục (CRUD, lọc tìm kiếm, ẩn/hiện, cascade soft-delete)
/// </summary>
public class AdminCategoryService : IAdminCategoryService
{
    private readonly ElectronicStoreDbContext _context;

    public AdminCategoryService(ElectronicStoreDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetAllCategoriesAsync(string? search = null)
    {
        var query = _context.Categories.Include(c => c.Products).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(kw) ||
                                     (c.Description != null && c.Description.ToLower().Contains(kw)));
        }

        return await query.OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(bool Success, string Message)> SaveCategoryAsync(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return (false, "Tên ngành hàng không được để trống!");
        }

        if (category.Id == 0)
        {
            category.Name = category.Name.Trim();
            category.Description = category.Description?.Trim();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return (true, $"Đã thêm danh mục mới: {category.Name}");
        }
        else
        {
            var existing = await _context.Categories.FindAsync(category.Id);
            if (existing == null)
            {
                return (false, "Không tìm thấy danh mục cần cập nhật!");
            }

            existing.Name = category.Name.Trim();
            existing.Description = category.Description?.Trim();
            existing.IsActive = category.IsActive;
            await _context.SaveChangesAsync();
            return (true, $"Đã cập nhật danh mục: {existing.Name}");
        }
    }

    public async Task<(bool Success, string Message)> ToggleCategoryStatusAsync(int id)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            return (false, "Không tìm thấy danh mục!");
        }

        // Đảo trạng thái hiển thị; khi ẩn danh mục thì toàn bộ sản phẩm liên kết tự động bị ẩn trên End-User
        category.IsActive = !category.IsActive;
        await _context.SaveChangesAsync();

        var prodCount = category.Products?.Count ?? 0;
        var message = category.IsActive
            ? $"Đã hiển thị lại danh mục '{category.Name}' và các sản phẩm thuộc danh mục này trên trang bán hàng!"
            : $"Đã tạm ẩn danh mục '{category.Name}' cùng toàn bộ {prodCount} sản phẩm thuộc danh mục này khỏi trang khách hàng!";

        return (true, message);
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            return (false, "Không tìm thấy danh mục!");
        }

        if (category.Products != null && category.Products.Count > 0)
        {
            // Tự động chuyển sang Tạm ẩn (Soft Delete) nếu có sản phẩm liên kết
            category.IsActive = false;
            await _context.SaveChangesAsync();
            return (true, $"Danh mục '{category.Name}' đang có {category.Products.Count} sản phẩm liên kết nên đã được chuyển sang trạng thái TẠM ẨN (toàn bộ sản phẩm thuộc danh mục này cũng được ẩn khỏi trang khách hàng).");
        }
        else
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return (true, $"Đã xóa vĩnh viễn danh mục '{category.Name}'.");
        }
    }
}

