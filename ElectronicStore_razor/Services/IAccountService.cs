using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Services;

/// <summary>
/// Service quản lý tài khoản và định danh dành riêng cho End-User (Khách mua hàng)
/// </summary>
public interface IAccountService
{
    Task<(bool Success, string Message, ApplicationUser? User, bool IsAdminBlocked)> ValidateCustomerLoginAsync(string usernameOrEmail, string password);
    Task<(bool Success, string Message, ApplicationUser? User)> RegisterAsync(string username, string fullName, string email, string phone, string address, string password);
    Task<ApplicationUser?> GetUserByIdOrNameAsync(string idOrName);
    Task<(bool Success, string Message)> UpdateProfileAsync(string userId, string fullName, string phone, string address);
    Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
}
