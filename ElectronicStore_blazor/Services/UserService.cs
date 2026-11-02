using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services;

public class UserService
{
    private const string StorageKey = "electro_auth_user";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SemaphoreSlim _lock = new(1, 1);

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
    /// Khôi phục trạng thái đăng nhập từ localStorage sau khi người dùng reload trang (F5).
    /// Dùng SemaphoreSlim để tránh xung đột DbContext khi Layout và Page cùng gọi ở OnAfterRenderAsync.
    /// </summary>
    public async Task<bool> RestoreSessionAsync(IJSRuntime js)
    {
        if (IsAuthenticated) return true;

        await _lock.WaitAsync();
        try
        {
            if (IsAuthenticated) return true;

            var savedUsername = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(savedUsername))
            {
                var user = await _userManager.FindByNameAsync(savedUsername.Trim());
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
                    await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                }
            }
        }
        catch
        {
            // Bỏ qua ngoại lệ JSInterop nếu đang ở bước prerender tĩnh hoặc ngắt kết nối.
        }
        finally
        {
            _lock.Release();
        }

        return false;
    }

    /// <summary>
    /// Xác thực đăng nhập bằng UserManager.CheckPasswordAsync (An toàn tuyệt đối trong SignalR Circuit, không phụ thuộc HttpContext)
    /// </summary>
    public async Task<(bool Success, string Message)> LoginAsync(string username, string password, IJSRuntime? js = null)
    {
        await _lock.WaitAsync();
        try
        {
            var user = await _userManager.FindByNameAsync(username.Trim());
            if (user == null)
            {
                return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!");
            }

            // BẢO MẬT & PHÂN TÁCH CỔNG: Chặn tuyệt đối tài khoản Admin đăng nhập tại cổng End-User
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                return (false, "Tên đăng nhập hoặc mật khẩu không chính xác!");
            }

            if (!user.IsActive)
            {
                return (false, "Tài khoản của bạn đã bị Admin KHÓA! Vui lòng liên hệ ban quản trị.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, password.Trim());
            if (!passwordValid)
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
                    await js.InvokeVoidAsync("localStorage.setItem", StorageKey, user.UserName);
                }
                catch { }
            }

            OnChange?.Invoke();
            return (true, "Đăng nhập thành công!");
        }
        finally
        {
            _lock.Release();
        }
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
        await _lock.WaitAsync();
        try
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

            var passwordValid = await _userManager.CheckPasswordAsync(user, password.Trim());
            if (!passwordValid)
            {
                return (false, "Tài khoản hoặc mật khẩu quản trị không chính xác!");
            }

            CurrentRole = "Admin";
            CurrentUser = user;

            if (js != null)
            {
                try
                {
                    await js.InvokeVoidAsync("localStorage.setItem", StorageKey, user.UserName);
                }
                catch { }
            }

            OnChange?.Invoke();
            return (true, "Xác thực quyền quản trị thành công!");
        }
        finally
        {
            _lock.Release();
        }
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

    /// <summary>
    /// Thực thi an toàn các thao tác UserManager trên cùng một Scoped DbContext bằng SemaphoreSlim
    /// để ngăn lỗi "A second operation was started on this context instance before a previous operation completed".
    /// </summary>
    public async Task<T> ExecuteWithLockAsync<T>(Func<UserManager<ApplicationUser>, Task<T>> action)
    {
        await _lock.WaitAsync();
        try
        {
            return await action(_userManager);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ExecuteWithLockAsync(Func<UserManager<ApplicationUser>, Task> action)
    {
        await _lock.WaitAsync();
        try
        {
            await action(_userManager);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task LogoutAsync(IJSRuntime? js = null)
    {
        CurrentUser = null;
        CurrentRole = string.Empty;

        if (js != null)
        {
            try
            {
                await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            }
            catch { }
        }

        OnChange?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        CurrentRole = string.Empty;
        OnChange?.Invoke();
    }
    /// <summary>
    /// Đăng ký tài khoản khách hàng mới (Chạy an toàn với Semaphore lock)
    /// </summary>
    public async Task<(bool Succeeded, string Error)> RegisterAsync(
        string username, string password, string fullName, string phoneNumber, string? email, string? address)
    {
        return await ExecuteWithLockAsync(async userManager =>
        {
            var existingUser = await userManager.FindByNameAsync(username.Trim());
            if (existingUser != null)
            {
                return (false, "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!");
            }

            var user = new ApplicationUser
            {
                UserName = username.Trim(),
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? $"{username.Trim()}@electrostore.vn" : email.Trim(),
                Address = string.IsNullOrWhiteSpace(address) ? "Chưa cập nhật địa chỉ" : address.Trim(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var result = await userManager.CreateAsync(user, password.Trim());
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Customer");
                return (true, string.Empty);
            }

            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        });
    }

    /// <summary>
    /// Cập nhật thông tin hồ sơ và đổi mật khẩu khách hàng
    /// </summary>
    public async Task<(bool Succeeded, string Error)> UpdateProfileAsync(
        string userId, string fullName, string email, string? phoneNumber, string? address, string? currentPassword, string? newPassword)
    {
        return await ExecuteWithLockAsync(async userManager =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "Không tìm thấy thông tin tài khoản!");
            }

            user.FullName = fullName.Trim();
            user.Email = email.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            user.Address = string.IsNullOrWhiteSpace(address) || address.Trim() == "Chưa cập nhật địa chỉ"
                ? null
                : address.Trim();

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    return (false, "Vui lòng nhập mật khẩu hiện tại để đổi mật khẩu mới.");
                }

                var changePassResult = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!changePassResult.Succeeded)
                {
                    return (false, string.Join("; ", changePassResult.Errors.Select(e => e.Description)));
                }
            }

            var updateResult = await userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                UpdateCurrentUser(user);
                return (true, string.Empty);
            }

            return (false, string.Join("; ", updateResult.Errors.Select(e => e.Description)));
        });
    }

}
