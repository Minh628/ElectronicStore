using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Services;

public class UserWithRoleDto
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
}

/// <summary>
/// Service quản lý tài khoản người dùng và xác thực (Account & Auth Domain)
/// </summary>
public interface IAccountService
{
    // ================= XÁC THỰC & KHÁCH HÀNG (END-USER) =================
    /// <summary>
    /// Xác thực đăng nhập cổng khách hàng (Chặn đứng quyền Admin với thông báo giả lập chống Username Enumeration)
    /// </summary>
    Task<(bool Success, string Message, ApplicationUser? User, bool IsAdminBlocked)> ValidateCustomerLoginAsync(string usernameOrEmail, string password);

    /// <summary>
    /// Xác thực đăng nhập cổng quản trị Admin
    /// </summary>
    Task<(bool Success, string Message, ApplicationUser? User)> ValidateAdminLoginAsync(string usernameOrEmail, string password);

    /// <summary>
    /// Đăng ký tài khoản khách hàng mới
    /// </summary>
    Task<(bool Success, string Message, ApplicationUser? User)> RegisterAsync(string username, string fullName, string email, string phone, string address, string password);

    /// <summary>
    /// Tìm người dùng theo ID hoặc UserName
    /// </summary>
    Task<ApplicationUser?> GetUserByIdOrNameAsync(string idOrName);

    /// <summary>
    /// Cập nhật thông tin hồ sơ người dùng
    /// </summary>
    Task<(bool Success, string Message)> UpdateProfileAsync(string userId, string fullName, string phone, string address);

    /// <summary>
    /// Thay đổi mật khẩu người dùng
    /// </summary>
    Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    // ================= QUẢN TRỊ VIÊN (ADMIN) =================
    /// <summary>
    /// Lấy danh sách tài khoản quản trị kèm vai trò và trạng thái khóa
    /// </summary>
    Task<List<UserWithRoleDto>> GetAdminAccountsAsync(string? search = null);

    /// <summary>
    /// Admin tạo tài khoản mới với vai trò chỉ định
    /// </summary>
    Task<(bool Success, string Message)> CreateAccountAsync(string username, string fullName, string email, string phone, string address, string password, string role);

    /// <summary>
    /// Admin cập nhật thông tin tài khoản (chống tự hạ quyền của chính mình)
    /// </summary>
    Task<(bool Success, string Message)> UpdateAccountAsync(string currentAdminId, string userId, string fullName, string email, string phone, string address, string role);

    /// <summary>
    /// Admin xóa tài khoản (chống tự xóa; tự động khóa vĩnh viễn nếu đã có lịch sử đơn hàng)
    /// </summary>
    Task<(bool Success, string Message)> DeleteAccountAsync(string currentAdminId, string userId);

    /// <summary>
    /// Admin bật/tắt khóa tài khoản (chống tự khóa chính mình)
    /// </summary>
    Task<(bool Success, string Message)> ToggleAccountLockAsync(string currentAdminId, string userId);
}

