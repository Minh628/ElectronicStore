using Microsoft.AspNetCore.Identity;
using ElectronicStore_razor.Data;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ElectronicStoreDbContext _context;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        ElectronicStoreDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<(bool Success, string Message, ApplicationUser? User, bool IsAdminBlocked)> ValidateCustomerLoginAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!", null, false);
        }

        var normalizedInput = usernameOrEmail.Trim();
        var user = await _userManager.FindByNameAsync(normalizedInput) ??
                   await _userManager.FindByEmailAsync(normalizedInput);

        if (user == null)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!", null, false);
        }

        var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, password.Trim());
        if (!isPasswordCorrect)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!", null, false);
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!", null, true);
        }

        return (true, "Xác thực thành công", user, false);
    }

    public async Task<(bool Success, string Message, ApplicationUser? User)> RegisterAsync(
        string username, string fullName, string email, string phone, string address, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Tên đăng nhập và mật khẩu không được để trống!", null);
        }

        var existingUser = await _userManager.FindByNameAsync(username.Trim());
        if (existingUser != null)
        {
            return (false, $"Tên đăng nhập '{username}' đã tồn tại!", null);
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

        var result = await _userManager.CreateAsync(user, password.Trim());
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ?? "Đăng ký không thành công!";
            return (false, firstError, null);
        }

        await _userManager.AddToRoleAsync(user, "Customer");
        return (true, "Đăng ký tài khoản thành công!", user);
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

        user.FullName = fullName.Trim();
        user.PhoneNumber = phone?.Trim();
        user.Address = address?.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, "Lỗi khi cập nhật thông tin!");
        }

        return (true, "Cập nhật hồ sơ thành công!");
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
