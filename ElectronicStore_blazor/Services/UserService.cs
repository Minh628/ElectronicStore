using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

public class UserService
{
    private const string AuthStorageKey = "electro_auth_user";
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public ApplicationUser? CurrentUser { get; private set; }
    public string CurrentRole { get; private set; } = string.Empty;
    public bool IsAuthenticated => CurrentUser != null;
    public bool IsAdmin => CurrentUser != null && CurrentRole == "Admin";

    public event Action? OnChange;

    /// <summary>
    /// Khôi phục trạng thái đăng nhập từ localStorage của trình duyệt khi người dùng reload trang (F5)
    /// </summary>
    public async Task<bool> RestoreSessionAsync(IJSRuntime js)
    {
        if (IsAuthenticated) return true;

        try
        {
            var username = await js.InvokeAsync<string?>("localStorage.getItem", AuthStorageKey);
            if (!string.IsNullOrWhiteSpace(username))
            {
                var user = await _userManager.FindByNameAsync(username.Trim());
                if (user != null && user.IsActive)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    CurrentRole = roles.FirstOrDefault() ?? "Customer";
                    CurrentUser = user;
                    OnChange?.Invoke();
                    return true;
                }
                else
                {
                    // Tài khoản không tồn tại hoặc đã bị khóa -> xóa localStorage
                    await js.InvokeVoidAsync("localStorage.removeItem", AuthStorageKey);
                }
            }
        }
        catch
        {
            // Bỏ qua ngoại lệ nếu JSInterop chưa sẵn sàng
        }

        return false;
    }

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password, IJSRuntime? js = null)
    {
        var user = await _userManager.FindByNameAsync(username.Trim());
        if (user == null)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!");
        }

        if (!user.IsActive)
        {
            return (false, "Tài khoản của bạn đã bị Admin KHÓA! Vui lòng liên hệ ban quản trị.");
        }

        var checkPassword = await _userManager.CheckPasswordAsync(user, password.Trim());
        if (!checkPassword)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!");
        }

        var roles = await _userManager.GetRolesAsync(user);
        CurrentRole = roles.FirstOrDefault() ?? "Customer";
        CurrentUser = user;

        if (js != null)
        {
            try
            {
                await js.InvokeVoidAsync("localStorage.setItem", AuthStorageKey, user.UserName);
            }
            catch {}
        }

        OnChange?.Invoke();
        return (true, "Đăng nhập thành công!");
    }

    public (bool Success, string Message) Login(string username, string password)
    {
        return LoginAsync(username, password).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Phương thức đăng nhập dành riêng cho Cổng Quản Trị (Admin Portal)
    /// </summary>
    public async Task<(bool Success, string Message)> AdminLoginAsync(string username, string password, IJSRuntime? js = null)
    {
        var user = await _userManager.FindByNameAsync(username.Trim());
        if (user == null)
        {
            return (false, "Tài khoản hoặc mật khẩu quản trị không chính xác!");
        }

        // KIỂM TRA ROLE ADMIN: Chặn hoàn toàn tài khoản khách hàng thông thường
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            return (false, "Truy cập bị từ chối: Tài khoản của bạn không có quyền truy cập vào Cổng Quản Trị Hệ Thống!");
        }

        if (!user.IsActive)
        {
            return (false, "Tài khoản quản trị viên này hiện đang bị tạm khóa!");
        }

        var checkPassword = await _userManager.CheckPasswordAsync(user, password.Trim());
        if (!checkPassword)
        {
            return (false, "Tài khoản hoặc mật khẩu quản trị không chính xác!");
        }

        CurrentRole = "Admin";
        CurrentUser = user;

        if (js != null)
        {
            try
            {
                await js.InvokeVoidAsync("localStorage.setItem", AuthStorageKey, user.UserName);
            }
            catch {}
        }

        OnChange?.Invoke();
        return (true, "Xác thực quyền quản trị thành công!");
    }

    public (bool Success, string Message) AdminLogin(string username, string password)
    {
        return AdminLoginAsync(username, password).GetAwaiter().GetResult();
    }

    public void UpdateCurrentUser(ApplicationUser user)
    {
        CurrentUser = user;
        OnChange?.Invoke();
    }

    public async Task LogoutAsync(IJSRuntime? js = null)
    {
        CurrentUser = null;
        CurrentRole = string.Empty;

        if (js != null)
        {
            try
            {
                await js.InvokeVoidAsync("localStorage.removeItem", AuthStorageKey);
            }
            catch {}
        }

        OnChange?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        CurrentRole = string.Empty;
        OnChange?.Invoke();
    }
}
