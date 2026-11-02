using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Areas.Admin.Models;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Areas.Admin.Services;

/// <summary>
/// Triển khai nghiệp vụ quản trị tài khoản Admin
/// </summary>
public class AdminAccountService : IAdminAccountService
{
    private readonly ElectronicStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminAccountService(
        ElectronicStoreDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<(bool Success, string Message, ApplicationUser? User)> ValidateAdminLoginAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu quản trị!", null);
        }

        var user = await _userManager.FindByNameAsync(usernameOrEmail) ??
                   await _userManager.FindByEmailAsync(usernameOrEmail);

        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return (false, "Thông tin quản trị viên không chính xác!", null);
        }

        if (await _userManager.IsLockedOutAsync(user) || !user.IsActive)
        {
            return (false, "Tài khoản quản trị này đã bị khóa!", null);
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            return (false, "Tài khoản này không có quyền truy cập khu vực quản trị!", null);
        }

        return (true, "Xác thực quản trị viên thành công!", user);
    }

    public async Task<string> GetCurrentAdminDisplayNameAsync(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);
        var fullName = user?.FullName;
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Contains("Quá") || fullName.Contains("Ã") || fullName.Contains("»") || fullName.Contains("Ä"))
        {
            return "Quản Trị Viên Hệ Thống";
        }
        return fullName;
    }

    public string GetCurrentAdminId(ClaimsPrincipal principal)
    {
        return _userManager.GetUserId(principal) ?? string.Empty;
    }

    public async Task<List<UserAccountViewModel>> GetAdminAccountsAsync(string? search = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(u => (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                                     (u.FullName != null && u.FullName.ToLower().Contains(kw)) ||
                                     (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                                     (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var dtos = new List<UserAccountViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            dtos.Add(new UserAccountViewModel
            {
                User = u,
                Role = roles.FirstOrDefault() ?? "Customer"
            });
        }

        return dtos;
    }

    public async Task<(bool Success, string Message)> CreateAccountAsync(
        string username, string fullName, string email, string phone, string address, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
        {
            return (false, "Vui lòng nhập đầy đủ Tên đăng nhập, Email và Mật khẩu!");
        }

        var existingUser = await _userManager.FindByNameAsync(username);
        if (existingUser != null)
        {
            return (false, $"Tên đăng nhập '{username}' đã tồn tại!");
        }

        var user = new ApplicationUser
        {
            UserName = username.Trim(),
            Email = email.Trim(),
            FullName = fullName?.Trim() ?? username.Trim(),
            PhoneNumber = phone?.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? "Chưa cập nhật địa chỉ" : address.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var assignRole = string.IsNullOrWhiteSpace(role) ? "Customer" : role;
        if (!await _roleManager.RoleExistsAsync(assignRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(assignRole));
        }
        await _userManager.AddToRoleAsync(user, assignRole);

        return (true, $"Đã tạo mới tài khoản '{user.UserName}' thành công với vai trò {assignRole}!");
    }

    public async Task<(bool Success, string Message)> UpdateAccountAsync(
        string currentAdminId, string userId, string fullName, string email, string phone, string address, string role)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? await _userManager.FindByNameAsync(userId);
        if (user == null)
        {
            return (false, "Không tìm thấy tài khoản cần cập nhật!");
        }

        // Chống tự hạ quyền của chính Admin đang thao tác
        if (user.Id == currentAdminId && role != "Admin")
        {
            return (false, "Không thể tự hạ quyền Admin của chính mình!");
        }

        user.FullName = fullName?.Trim() ?? user.FullName;
        user.Email = email?.Trim() ?? user.Email;
        user.PhoneNumber = phone?.Trim();
        user.Address = string.IsNullOrWhiteSpace(address) ? user.Address : address.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, string.Join("; ", updateResult.Errors.Select(e => e.Description)));
        }

        // Cập nhật vai trò
        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetRole = string.IsNullOrWhiteSpace(role) ? "Customer" : role;
        if (!currentRoles.Contains(targetRole))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!await _roleManager.RoleExistsAsync(targetRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(targetRole));
            }
            await _userManager.AddToRoleAsync(user, targetRole);
        }

        return (true, $"Đã cập nhật thông tin tài khoản '{user.UserName}' thành công!");
    }

    public async Task<(bool Success, string Message)> DeleteAccountAsync(string currentAdminId, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? await _userManager.FindByNameAsync(userId);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng!");
        }

        if (user.Id == currentAdminId)
        {
            return (false, "Không thể tự xóa tài khoản của chính mình!");
        }

        // Kiểm tra xem tài khoản đã phát sinh đơn hàng hay chưa
        bool hasOrders = await _context.Orders.AnyAsync(o => o.UserId == user.Id);
        if (hasOrders)
        {
            // Soft Lock: Đổi IsActive = false và SetLockoutEndDate để khóa vĩnh viễn
            user.IsActive = false;
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            await _userManager.UpdateAsync(user);
            return (true, $"Tài khoản '{user.UserName}' đã có lịch sử đơn hàng. Hệ thống đã tự động chuyển sang trạng thái KHÓA VĨNH VIỄN để bảo toàn dữ liệu kế toán.");
        }
        else
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
            return (true, $"Đã xóa vĩnh viễn tài khoản '{user.UserName}'.");
        }
    }

    public async Task<(bool Success, string Message)> ToggleAccountLockAsync(string currentAdminId, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? await _userManager.FindByNameAsync(userId);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng!");
        }

        if (user.Id == currentAdminId)
        {
            return (false, "Không thể tự khóa tài khoản của chính mình!");
        }

        // Đồng bộ cả IsActive và LockoutEnd
        user.IsActive = !user.IsActive;
        await _userManager.SetLockoutEndDateAsync(user, user.IsActive ? null : DateTimeOffset.MaxValue);
        await _userManager.UpdateAsync(user);

        return (true, user.IsActive 
            ? $"Đã mở khóa cho tài khoản '{user.UserName}'!" 
            : $"Đã khóa tài khoản '{user.UserName}'!");
    }
}

