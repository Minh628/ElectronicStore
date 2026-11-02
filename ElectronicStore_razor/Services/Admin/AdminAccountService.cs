using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services.Admin;

public class AdminAccountService : IAdminAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ElectronicStoreDbContext _context;

    public AdminAccountService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ElectronicStoreDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<(bool Success, string Message, ApplicationUser? User)> ValidateAdminLoginAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!", null);
        }

        var normalizedInput = usernameOrEmail.Trim();
        var user = await _userManager.FindByNameAsync(normalizedInput) ??
                   await _userManager.FindByEmailAsync(normalizedInput);

        if (user == null)
        {
            return (false, "Tài khoản hoặc mật khẩu quản trị không chính xác!", null);
        }

        var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, password.Trim());
        if (!isPasswordCorrect)
        {
            return (false, "Tài khoản hoặc mật khẩu quản trị không chính xác!", null);
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            return (false, "Tài khoản của bạn không có quyền truy cập khu vực Quản trị!", null);
        }

        return (true, "Xác thực thành công", user);
    }

    public async Task<string> GetCurrentAdminDisplayNameAsync(ClaimsPrincipal principal)
    {
        var currentUser = await _userManager.GetUserAsync(principal);
        var fullName = currentUser?.FullName;
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Contains("Quá") || fullName.Contains("Ã") || fullName.Contains("»") || fullName.Contains("Ä"))
        {
            fullName = "Quản Trị Viên Hệ Thống";
        }
        return fullName;
    }

    public string GetCurrentAdminId(ClaimsPrincipal principal)
    {
        return _userManager.GetUserId(principal) ?? string.Empty;
    }

    public async Task<List<UserWithRoleDto>> GetAdminAccountsAsync(string? search = null)
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

        var users = await query.ToListAsync();
        var result = new List<UserWithRoleDto>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var isLocked = !u.IsActive || (u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);

            result.Add(new UserWithRoleDto
            {
                User = u,
                Role = roles.FirstOrDefault() ?? "Customer",
                IsLocked = isLocked
            });
        }

        return result;
    }

    public async Task<(bool Success, string Message)> CreateAccountAsync(
        string username, string fullName, string email, string phone, string address, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Tên đăng nhập và mật khẩu không được để trống!");
        }

        var existingUser = await _userManager.FindByNameAsync(username.Trim());
        if (existingUser != null)
        {
            return (false, $"Tên đăng nhập '{username}' đã tồn tại!");
        }

        var user = new ApplicationUser
        {
            UserName = username.Trim(),
            FullName = string.IsNullOrWhiteSpace(fullName) ? username.Trim() : fullName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? $"{username.Trim()}@electrostore.vn" : email.Trim(),
            PhoneNumber = phone?.Trim(),
            Address = address?.Trim(),
            IsActive = true,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, password.Trim());
        if (!createResult.Succeeded)
        {
            var firstError = createResult.Errors.FirstOrDefault()?.Description ?? "Tạo tài khoản thất bại!";
            return (false, firstError);
        }

        var targetRole = string.IsNullOrWhiteSpace(role) ? "Customer" : role.Trim();
        if (!await _roleManager.RoleExistsAsync(targetRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(targetRole));
        }

        await _userManager.AddToRoleAsync(user, targetRole);
        return (true, $"Tạo tài khoản '{user.UserName}' với quyền '{targetRole}' thành công!");
    }

    public async Task<(bool Success, string Message)> UpdateAccountAsync(
        string currentAdminId, string userId, string fullName, string email, string phone, string address, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng!");
        }

        user.FullName = fullName?.Trim() ?? user.FullName;
        user.Email = email?.Trim() ?? user.Email;
        user.PhoneNumber = phone?.Trim() ?? user.PhoneNumber;
        user.Address = address?.Trim() ?? user.Address;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, "Lỗi khi cập nhật thông tin cá nhân!");
        }

        // Cập nhật vai trò (Role)
        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetRole = string.IsNullOrWhiteSpace(role) ? "Customer" : role.Trim();

        if (!currentRoles.Contains(targetRole))
        {
            if (user.Id == currentAdminId && targetRole != "Admin")
            {
                return (false, "Cảnh báo bảo mật: Bạn không thể tự hạ quyền Admin của chính mình!");
            }

            if (!await _roleManager.RoleExistsAsync(targetRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(targetRole));
            }

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, targetRole);
        }

        return (true, $"Cập nhật tài khoản '{user.UserName}' thành công!");
    }

    public async Task<(bool Success, string Message)> DeleteAccountAsync(string currentAdminId, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "Không tìm thấy tài khoản!");
        }

        if (user.Id == currentAdminId)
        {
            return (false, "Bạn không thể tự xóa tài khoản của chính mình!");
        }

        var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == user.Id);
        if (hasOrders)
        {
            user.IsActive = false;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(user);
            return (true, $"Tài khoản '{user.UserName}' đã phát sinh đơn hàng trong quá khứ nên hệ thống đã tự động KHÓA VĨNH VIỄN để bảo toàn lịch sử!");
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            return (false, "Lỗi hệ thống khi xóa tài khoản!");
        }

        return (true, $"Đã xóa vĩnh viễn tài khoản '{user.UserName}' thành công!");
    }

    public async Task<(bool Success, string Message)> ToggleAccountLockAsync(string currentAdminId, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng!");
        }

        if (user.Id == currentAdminId)
        {
            return (false, "Bạn không thể tự khóa tài khoản Admin của chính mình!");
        }

        user.IsActive = !user.IsActive;
        user.LockoutEnd = user.IsActive ? null : DateTimeOffset.MaxValue;
        await _userManager.UpdateAsync(user);

        var statusText = user.IsActive ? "MỞ KHÓA" : "KHÓA";
        return (true, $"Đã {statusText} tài khoản '{user.UserName}' thành công!");
    }
}
