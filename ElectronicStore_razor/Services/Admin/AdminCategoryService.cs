using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services.Admin;

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
            return (false, "Tên danh mục không được để trống!");
        }

        var isDuplicate = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == category.Name.Trim().ToLower() && c.Id != category.Id);

        if (isDuplicate)
        {
            return (false, $"Tên danh mục '{category.Name}' đã tồn tại trong hệ thống!");
        }

        if (category.Id == 0)
        {
            category.Name = category.Name.Trim();
            category.Description = category.Description?.Trim();
            category.IsActive = true;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return (true, $"Thêm mới danh mục '{category.Name}' thành công!");
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
            return (true, $"Cập nhật danh mục '{existing.Name}' thành công!");
        }
    }

    public async Task<(bool Success, string Message)> ToggleCategoryStatusAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return (false, "Không tìm thấy danh mục!");
        }

        category.IsActive = !category.IsActive;
        await _context.SaveChangesAsync();

        var statusText = category.IsActive ? "HIỂN THỊ" : "ẨN";
        return (true, $"Đã chuyển trạng thái danh mục '{category.Name}' sang: {statusText}!");
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return (false, "Không tìm thấy danh mục cần xóa!");
        }

        var hasOrders = await _context.OrderDetails
            .AnyAsync(od => od.Product.CategoryId == id);

        if (hasOrders)
        {
            category.IsActive = false;
            await _context.SaveChangesAsync();
            return (false, $"Danh mục '{category.Name}' đã có sản phẩm phát sinh đơn hàng, hệ thống đã chuyển sang chế độ TẠM ẨN để bảo toàn dữ liệu!");
        }

        if (category.Products != null && category.Products.Any())
        {
            _context.Products.RemoveRange(category.Products);
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return (true, $"Đã xóa vĩnh viễn danh mục '{category.Name}' thành công!");
    }
}
