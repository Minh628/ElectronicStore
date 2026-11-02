using ElectronicStore_blazor.Models.Entities;

namespace ElectronicStore_blazor.Services.Admin;

/// <summary>
/// Service quản lý tài khoản và phân quyền người dùng dành riêng cho phân hệ Quản trị (Admin)
/// </summary>
public interface IAdminAccountService
{
    Task<List<ApplicationUser>> GetUsersAsync(string? search = null);
    Task<Dictionary<string, string>> GetUserRolesAsync(List<ApplicationUser> users);
    Task<(bool Success, string Message)> CreateAccountAsync(string username, string fullName, string email, string phone, string address, string password, string role);
    Task<(bool Success, string Message)> UpdateAccountAsync(string currentAdminUsername, ApplicationUser user, string fullName, string email, string phone, string address, string role);
    Task<(bool Success, string Message)> DeleteAccountAsync(string currentAdminUsername, ApplicationUser user);
    Task<(bool Success, string Message)> ToggleLockAsync(string currentAdminUsername, ApplicationUser user);
}
