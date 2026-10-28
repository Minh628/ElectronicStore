using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Areas.Admin.Models;

/// <summary>
/// ViewModel quản lý tài khoản người dùng trong phân hệ quản trị
/// </summary>
public class UserAccountViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = "Customer";
}

