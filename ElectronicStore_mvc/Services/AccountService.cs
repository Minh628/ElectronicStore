using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_mvc.Data;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

/// <summary>
/// Triển khai nghiệp vụ tài khoản và phân quyền
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

        // Kiểm tra mật khẩu
        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!", null, false);
        }

        // Kiểm tra tài khoản có bị khóa hay không
        if (await _userManager.IsLockedOutAsync(user))
        {
            return (false, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên!", null, false);
        }

        // BẢO MẬT PHÂN TÁCH CỔNG: Chặn tài khoản Admin tại cổng End-User với thông báo giả lập chống Username Enumeration
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!", null, true);
        }

        return (true, "Đăng nhập thành công!", user, false);
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

        if (await _userManager.IsLockedOutAsync(user))
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
            FullName = fullName?.Trim() ?? username.Trim(),
            PhoneNumber = phone?.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? "Chưa cập nhật địa chỉ" : address.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ?? "Đăng ký không thành công.";
            return (false, firstError, null);
        }

        // Gán quyền Customer mặc định
        if (!await _roleManager.RoleExistsAsync("Customer"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Customer"));
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

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var dtos = new List<UserWithRoleDto>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            bool isLocked = await _userManager.IsLockedOutAsync(u);
            dtos.Add(new UserWithRoleDto
            {
                User = u,
                Role = roles.FirstOrDefault() ?? "Customer",
                IsLocked = isLocked
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
            // Soft Lock: Khóa vĩnh viễn tài khoản để bảo toàn tính toàn vẹn dữ liệu kế toán và lịch sử hóa đơn
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
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

        var isLocked = await _userManager.IsLockedOutAsync(user);
        if (isLocked)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            return (true, $"Đã mở khóa cho tài khoản '{user.UserName}'!");
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            return (true, $"Đã khóa tài khoản '{user.UserName}'!");
        }
    }
}

