using System.Security.Claims;
using ElectronicStore_mvc.Areas.Admin.Models;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Areas.Admin.Services;

/// <summary>
/// Service chuyên trách quản trị người dùng, phân quyền và xác thực cổng Admin
/// </summary>
public interface IAdminAccountService
{
    Task<(bool Success, string Message, ApplicationUser? User)> ValidateAdminLoginAsync(string usernameOrEmail, string password);
    Task<string> GetCurrentAdminDisplayNameAsync(ClaimsPrincipal principal);
    string GetCurrentAdminId(ClaimsPrincipal principal);
    Task<List<UserAccountViewModel>> GetAdminAccountsAsync(string? search = null);
    Task<(bool Success, string Message)> CreateAccountAsync(string username, string fullName, string email, string phone, string address, string password, string role);
    Task<(bool Success, string Message)> UpdateAccountAsync(string currentAdminId, string userId, string fullName, string email, string phone, string address, string role);
    Task<(bool Success, string Message)> DeleteAccountAsync(string currentAdminId, string userId);
    Task<(bool Success, string Message)> ToggleAccountLockAsync(string currentAdminId, string userId);
}

