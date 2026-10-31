using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AccountsModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ElectronicStoreDbContext _context;

    public AccountsModel(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ElectronicStoreDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public List<ApplicationUser> Users { get; set; } = new();
    public Dictionary<string, string> UserRoles { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public async Task OnGetAsync()
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var kw = SearchKeyword.Trim().ToLower();
            query = query.Where(u => (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                                     (u.FullName != null && u.FullName.ToLower().Contains(kw)) ||
                                     (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                                     (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
        }

        Users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

        foreach (var u in Users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            UserRoles[u.Id] = roles.FirstOrDefault() ?? "Customer";
        }
    }

    /// <summary>
    /// Thêm tài khoản người dùng mới (Admin hoặc Khách hàng)
    /// </summary>
    public async Task<IActionResult> OnPostCreateUserAsync(string username, string fullName, string email, string phone, string address, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            AlertMessage = "Tên đăng nhập và mật khẩu không được để trống!";
            AlertType = "danger";
            return RedirectToPage(new { SearchKeyword });
        }

        var existing = await _userManager.FindByNameAsync(username.Trim());
        if (existing != null)
        {
            AlertMessage = $"Tên đăng nhập '{username}' đã được sử dụng!";
            AlertType = "danger";
            return RedirectToPage(new { SearchKeyword });
        }

        var user = new ApplicationUser
        {
            UserName = username.Trim(),
            FullName = string.IsNullOrWhiteSpace(fullName) ? username.Trim() : fullName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? $"{username.Trim()}@electrostore.vn" : email.Trim(),
            PhoneNumber = phone?.Trim(),
            Address = address?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            AlertMessage = "Lỗi khi tạo tài khoản: " + string.Join(", ", result.Errors.Select(e => e.Description));
            AlertType = "danger";
            return RedirectToPage(new { SearchKeyword });
        }

        // Đảm bảo role tồn tại
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }
        await _userManager.AddToRoleAsync(user, role);

        AlertMessage = $"Đã tạo thành công tài khoản '{user.UserName}' với vai trò '{role}'!";
        AlertType = "success";
        return RedirectToPage(new { SearchKeyword });
    }

    /// <summary>
    /// Cập nhật thông tin và vai trò của tài khoản
    /// </summary>
    public async Task<IActionResult> OnPostUpdateUserAsync(string userId, string fullName, string email, string phone, string address, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            AlertMessage = "Không tìm thấy người dùng cần sửa!";
            AlertType = "danger";
            return RedirectToPage(new { SearchKeyword });
        }

        // Chặn Admin tự tước quyền quản trị của chính mình
        if (User.Identity != null && User.Identity.Name == user.UserName && role != "Admin")
        {
            AlertMessage = "Bạn không thể tự hạ quyền Quản trị viên của chính mình!";
            AlertType = "warning";
            return RedirectToPage(new { SearchKeyword });
        }

        user.FullName = fullName?.Trim();
        user.Email = email?.Trim();
        user.PhoneNumber = phone?.Trim();
        user.Address = address?.Trim();

        await _userManager.UpdateAsync(user);

        // Cập nhật vai trò
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(role))
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);
        }

        AlertMessage = $"Đã cập nhật thông tin tài khoản '{user.UserName}'!";
        AlertType = "success";
        return RedirectToPage(new { SearchKeyword });
    }

    /// <summary>
    /// Xóa tài khoản (bảo toàn CSDL: nếu đã có đơn hàng thì chuyển sang Khóa tài khoản)
    /// </summary>
    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return RedirectToPage(new { SearchKeyword });

        if (User.Identity != null && User.Identity.Name == user.UserName)
        {
            AlertMessage = "Bạn không thể tự xóa tài khoản Admin của chính mình!";
            AlertType = "warning";
            return RedirectToPage(new { SearchKeyword });
        }

        // Kiểm tra xem người dùng đã từng phát sinh đơn hàng hay chưa
        bool hasOrders = await _context.Orders.AnyAsync(o => o.UserId == user.Id);
        if (hasOrders)
        {
            user.IsActive = false;
            await _userManager.UpdateAsync(user);
            AlertMessage = $"Tài khoản '{user.UserName}' đã có lịch sử đơn hàng trong hệ thống. Để bảo toàn dữ liệu kế toán & hóa đơn, hệ thống đã chuyển sang trạng thái KHÓA TÀI KHOẢN thay vì xóa cứng.";
            AlertType = "warning";
        }
        else
        {
            var res = await _userManager.DeleteAsync(user);
            if (res.Succeeded)
            {
                AlertMessage = $"Đã xóa vĩnh viễn tài khoản '{user.UserName}'.";
                AlertType = "success";
            }
            else
            {
                AlertMessage = "Không thể xóa: " + string.Join(", ", res.Errors.Select(e => e.Description));
                AlertType = "danger";
            }
        }

        return RedirectToPage(new { SearchKeyword });
    }

    /// <summary>
    /// Khóa hoặc Mở khóa tài khoản người dùng
    /// </summary>
    public async Task<IActionResult> OnPostToggleActiveAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return RedirectToPage(new { SearchKeyword });

        if (User.Identity != null && User.Identity.Name == user.UserName)
        {
            AlertMessage = "Bạn không thể tự khóa tài khoản Admin của chính mình!";
            AlertType = "warning";
            return RedirectToPage(new { SearchKeyword });
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        AlertMessage = user.IsActive 
            ? $"Đã MỞ KHÓA tài khoản '{user.UserName}' thành công!" 
            : $"Đã KHÓA tài khoản '{user.UserName}'! Người này sẽ không thể đăng nhập.";
        AlertType = user.IsActive ? "success" : "danger";

        return RedirectToPage(new { SearchKeyword });
    }
}
