using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicStore_blazor.Data;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services.Admin;

public class AdminAccountService : IAdminAccountService
{
    private readonly UserService _userService;
    private readonly IDbContextFactory<ElectronicStoreDbContext> _contextFactory;

    public AdminAccountService(
        UserService userService,
        IDbContextFactory<ElectronicStoreDbContext> contextFactory)
    {
        _userService = userService;
        _contextFactory = contextFactory;
    }

    public async Task<List<ApplicationUser>> GetUsersAsync(string? search = null)
    {
        return await _userService.ExecuteWithLockAsync(async userManager =>
        {
            var query = userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(u => (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                                         (u.FullName != null && u.FullName.ToLower().Contains(kw)) ||
                                         (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                                         (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
            }

            return await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        });
    }

    public async Task<Dictionary<string, string>> GetUserRolesAsync(List<ApplicationUser> users)
    {
        return await _userService.ExecuteWithLockAsync(async userManager =>
        {
            var dict = new Dictionary<string, string>();
            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                dict[u.Id] = roles.FirstOrDefault() ?? "Customer";
            }
            return dict;
        });
    }

    public async Task<(bool Success, string Message)> CreateAccountAsync(
        string username, string fullName, string email, string phone, string address, string password, string role)
    {
        if (role is not ("Customer" or "Admin"))
        {
            return (false, "Vai trò tài khoản không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Tên đăng nhập và mật khẩu không được để trống!");
        }

        return await _userService.ExecuteWithLockAsync(async userManager =>
        {
            var existing = await userManager.FindByNameAsync(username.Trim());
            if (existing != null)
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
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password.Trim());
            if (!result.Succeeded)
            {
                var err = string.Join("; ", result.Errors.Select(e => e.Description));
                return (false, "Lỗi khi tạo tài khoản: " + err);
            }

            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                var err = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                return (false, "Không thể gán vai trò: " + err);
            }

            return (true, $"Đã tạo thành công tài khoản '{user.UserName}' với vai trò '{role}'!");
        });
    }

    public async Task<(bool Success, string Message)> UpdateAccountAsync(
        string currentAdminUsername, ApplicationUser user, string fullName, string email, string phone, string address, string role)
    {
        if (role is not ("Customer" or "Admin"))
        {
            return (false, "Vai trò tài khoản không hợp lệ.");
        }

        if (user.UserName == currentAdminUsername && role != "Admin")
        {
            return (false, "Bạn không thể tự hạ quyền Quản trị viên của chính mình!");
        }

        return await _userService.ExecuteWithLockAsync(async userManager =>
        {
            var dbUser = await userManager.FindByIdAsync(user.Id);
            if (dbUser == null)
            {
                return (false, "Không tìm thấy người dùng!");
            }

            dbUser.FullName = fullName.Trim();
            dbUser.Email = string.IsNullOrWhiteSpace(email) ? $"{dbUser.UserName}@electrostore.vn" : email.Trim();
            dbUser.PhoneNumber = phone?.Trim();
            dbUser.Address = address?.Trim();

            await userManager.UpdateAsync(dbUser);

            var currentRoles = await userManager.GetRolesAsync(dbUser);
            if (!currentRoles.Contains(role))
            {
                await userManager.RemoveFromRolesAsync(dbUser, currentRoles);
                var roleResult = await userManager.AddToRoleAsync(dbUser, role);
                if (!roleResult.Succeeded)
                {
                    return (false, "Không thể cập nhật vai trò: " + string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            return (true, $"Đã cập nhật thông tin tài khoản '{dbUser.UserName}'!");
        });
    }

    public async Task<(bool Success, string Message)> DeleteAccountAsync(string currentAdminUsername, ApplicationUser user)
    {
        if (user.UserName == currentAdminUsername)
        {
            return (false, "Bạn không thể tự xóa tài khoản Admin của chính mình!");
        }

        using var context = _contextFactory.CreateDbContext();
        bool hasOrders = await context.Orders.AnyAsync(o => o.UserId == user.Id);

        return await _userService.ExecuteWithLockAsync(async userManager =>
        {
            var dbUser = await userManager.FindByIdAsync(user.Id);
            if (dbUser == null)
            {
                return (false, "Không tìm thấy người dùng!");
            }

            if (hasOrders)
            {
                dbUser.IsActive = false;
                dbUser.LockoutEnd = DateTimeOffset.MaxValue;
                await userManager.UpdateAsync(dbUser);
                return (true, $"Tài khoản '{dbUser.UserName}' đã có lịch sử đơn hàng. Hệ thống đã tự động chuyển sang trạng thái KHÓA VĨNH VIỄN để bảo toàn dữ liệu kế toán & hóa đơn.");
            }
            else
            {
                var res = await userManager.DeleteAsync(dbUser);
                if (res.Succeeded)
                {
                    return (true, $"Đã xóa vĩnh viễn tài khoản '{dbUser.UserName}'.");
                }
                else
                {
                    return (false, "Không thể xóa: " + string.Join("; ", res.Errors.Select(e => e.Description)));
                }
            }
        });
    }

    public async Task<(bool Success, string Message)> ToggleLockAsync(string currentAdminUsername, ApplicationUser user)
    {
        if (user.UserName == currentAdminUsername)
        {
            return (false, "Bạn không thể tự khóa tài khoản Admin của chính mình!");
        }

        return await _userService.ExecuteWithLockAsync(async userManager =>
        {
            var dbUser = await userManager.FindByIdAsync(user.Id);
            if (dbUser == null)
            {
                return (false, "Không tìm thấy người dùng!");
            }

            dbUser.IsActive = !dbUser.IsActive;
            dbUser.LockoutEnd = dbUser.IsActive ? null : DateTimeOffset.MaxValue;
            var result = await userManager.UpdateAsync(dbUser);

            if (result.Succeeded)
            {
                return (true, $"Đã {(dbUser.IsActive ? "MỞ KHÓA" : "KHÓA")} tài khoản {dbUser.UserName}!");
            }
            else
            {
                return (false, "Không thể cập nhật trạng thái tài khoản: " + string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        });
    }
}
