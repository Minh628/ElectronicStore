using System.Security.Claims;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services.Admin;

public class UserWithRoleDto
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
}

/// <summary>
/// Service quản lý tài khoản và quyền hạn dành riêng cho phân hệ Quản trị (Admin)
/// </summary>
public interface IAdminAccountService
{
    Task<(bool Success, string Message, ApplicationUser? User)> ValidateAdminLoginAsync(string usernameOrEmail, string password);
    Task<string> GetCurrentAdminDisplayNameAsync(ClaimsPrincipal principal);
    string GetCurrentAdminId(ClaimsPrincipal principal);
    Task<List<UserWithRoleDto>> GetAdminAccountsAsync(string? search = null);
    Task<(bool Success, string Message)> CreateAccountAsync(string username, string fullName, string email, string phone, string address, string password, string role);
    Task<(bool Success, string Message)> UpdateAccountAsync(string currentAdminId, string userId, string fullName, string email, string phone, string address, string role);
    Task<(bool Success, string Message)> DeleteAccountAsync(string currentAdminId, string userId);
    Task<(bool Success, string Message)> ToggleAccountLockAsync(string currentAdminId, string userId);
}
