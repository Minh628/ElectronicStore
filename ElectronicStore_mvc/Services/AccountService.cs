using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

/// <summary>
/// Triển khai nghiệp vụ tài khoản End-User
/// </summary>
public class AccountService : IAccountService
{
    private readonly ElectronicStoreDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountService(
        ElectronicStoreDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<(bool Success, string Message, ApplicationUser? User, bool IsAdminBlocked)> ValidateCustomerLoginAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Vui lòng nhập tên đăng nhập và mật khẩu!", null, false);
        }

        var user = await _userManager.FindByNameAsync(usernameOrEmail) ??
                   await _userManager.FindByEmailAsync(usernameOrEmail);

        if (user == null)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!", null, false);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!", null, false);
        }

        if (await _userManager.IsLockedOutAsync(user) || !user.IsActive)
        {
            return (false, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ ban quản trị để được hỗ trợ!", null, false);
        }

        // BẢO MẬT PHÂN TÁCH CỔNG: Chặn tài khoản Admin tại cổng End-User với thông báo giả lập chống Username Enumeration
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!", null, true);
        }

        return (true, "Đăng nhập thành công!", user, false);
    }

    public async Task<(bool Success, string Message, ApplicationUser? User)> RegisterAsync(
        string username, string fullName, string email, string phone, string address, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Vui lòng điền đầy đủ các thông tin bắt buộc!", null);
        }

        if (await _userManager.FindByNameAsync(username) != null)
        {
            return (false, "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!", null);
        }

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            return (false, "Địa chỉ email này đã được đăng ký!", null);
        }

        var user = new ApplicationUser
        {
            UserName = username.Trim(),
            Email = email.Trim(),
            FullName = string.IsNullOrWhiteSpace(fullName) ? username.Trim() : fullName.Trim(),
            PhoneNumber = phone?.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? "Chưa cập nhật địa chỉ" : address.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ?? "Đăng ký không thành công.";
            return (false, firstError, null);
        }

        if (!await _roleManager.RoleExistsAsync("Customer"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Customer"));
        }
        await _userManager.AddToRoleAsync(user, "Customer");

        return (true, "Đăng ký tài khoản thành công!", user);
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity == null || !principal.Identity.IsAuthenticated) return null;
        return await _userManager.GetUserAsync(principal);
    }

    public async Task<ApplicationUser?> GetUserByIdOrNameAsync(string idOrName)
    {
        return await _userManager.FindByIdAsync(idOrName) ??
               await _userManager.FindByNameAsync(idOrName);
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(string userId, string fullName, string phone, string address)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng!");
        }

        user.FullName = fullName?.Trim() ?? user.FullName;
        user.PhoneNumber = phone?.Trim() ?? user.PhoneNumber;
        user.Address = string.IsNullOrWhiteSpace(address) ? user.Address : address.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, result.Errors.FirstOrDefault()?.Description ?? "Cập nhật hồ sơ thất bại!");
        }

        return (true, "Cập nhật thông tin cá nhân thành công!");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng!");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            return (false, result.Errors.FirstOrDefault()?.Description ?? "Đổi mật khẩu thất bại!");
        }

        return (true, "Đổi mật khẩu thành công!");
    }
}
