using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

public class UserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public UserService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public ApplicationUser? CurrentUser { get; private set; }
    public string CurrentRole { get; private set; } = string.Empty;
    public bool IsAuthenticated => CurrentUser != null;
    public bool IsAdmin => CurrentUser != null && CurrentRole == "Admin";

    public event Action? OnChange;

    /// <summary>
    /// Khôi phục trạng thái đăng nhập từ Identity cookie sau khi người dùng reload trang.
    /// </summary>
    public async Task<bool> RestoreSessionAsync(IJSRuntime js)
    {
        if (IsAuthenticated) return true;

        try
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            if (state.User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(state.User);
                if (user != null && user.IsActive)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    CurrentRole = roles.FirstOrDefault() ?? "Customer";
                    CurrentUser = user;
                    OnChange?.Invoke();
                    return true;
                }
            }
        }
        catch
        {
            // Authentication state may not be available during prerendering.
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

        var result = await _signInManager.PasswordSignInAsync(
            user,
            password.Trim(),
            isPersistent: true,
            lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!");
        }

        var roles = await _userManager.GetRolesAsync(user);
        CurrentRole = roles.FirstOrDefault() ?? "Customer";
        CurrentUser = user;

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

        var result = await _signInManager.PasswordSignInAsync(
            user,
            password.Trim(),
            isPersistent: true,
            lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return (false, "Tài khoản hoặc mật khẩu quản trị không chính xác!");
        }

        CurrentRole = "Admin";
        CurrentUser = user;

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
        await _signInManager.SignOutAsync();
        CurrentUser = null;
        CurrentRole = string.Empty;

        OnChange?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        CurrentRole = string.Empty;
        OnChange?.Invoke();
    }
}
