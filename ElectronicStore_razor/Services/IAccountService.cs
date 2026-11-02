using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

public class UserWithRoleDto
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
}

/// <summary>
/// Service quản lý tài khoản người dùng và xác thực (Account & Auth Domain) cho Razor Pages
/// </summary>
public interface IAccountService
{
    // End-User
    Task<(bool Success, string Message, ApplicationUser? User, bool IsAdminBlocked)> ValidateCustomerLoginAsync(string usernameOrEmail, string password);
    Task<(bool Success, string Message, ApplicationUser? User)> ValidateAdminLoginAsync(string usernameOrEmail, string password);
    Task<(bool Success, string Message, ApplicationUser? User)> RegisterAsync(string username, string fullName, string email, string phone, string address, string password);
    Task<ApplicationUser?> GetUserByIdOrNameAsync(string idOrName);
    Task<(bool Success, string Message)> UpdateProfileAsync(string userId, string fullName, string phone, string address);
    Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    // Admin
    Task<List<UserWithRoleDto>> GetAdminAccountsAsync(string? search = null);
    Task<(bool Success, string Message)> CreateAccountAsync(string username, string fullName, string email, string phone, string address, string password, string role);
    Task<(bool Success, string Message)> UpdateAccountAsync(string currentAdminId, string userId, string fullName, string email, string phone, string address, string role);
    Task<(bool Success, string Message)> DeleteAccountAsync(string currentAdminId, string userId);
    Task<(bool Success, string Message)> ToggleAccountLockAsync(string currentAdminId, string userId);
}

