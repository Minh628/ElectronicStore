using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AccountsModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountsModel(
        IAccountService accountService,
        UserManager<ApplicationUser> userManager)
    {
        _accountService = accountService;
        _userManager = userManager;
    }

    public List<ApplicationUser> Users { get; set; } = new();
    public Dictionary<string, string> UserRoles { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchKeyword { get; set; }

    [TempData]
    public string? AlertMessage { get; set; }

    [TempData]
    public string? AlertType { get; set; }

    public async Task OnGetAsync()
    {
        var dtos = await _accountService.GetAdminAccountsAsync(SearchKeyword);
        Users = dtos.Select(d => d.User).ToList();
        UserRoles.Clear();
        foreach (var d in dtos)
        {
            UserRoles[d.User.Id] = d.Role;
        }
    }

    /// <summary>
    /// Thêm tài khoản người dùng mới qua AccountService
    /// </summary>
    public async Task<IActionResult> OnPostCreateUserAsync(string username, string fullName, string email, string phone, string address, string password, string role)
    {
        var result = await _accountService.CreateAccountAsync(username, fullName, email, phone, address, password, role);
        AlertMessage = result.Message;
        AlertType = result.Success ? "success" : "danger";
        return RedirectToPage(new { SearchKeyword });
    }

    /// <summary>
    /// Cập nhật thông tin và vai trò của tài khoản qua AccountService
    /// </summary>
    public async Task<IActionResult> OnPostUpdateUserAsync(string userId, string fullName, string email, string phone, string address, string role)
    {
        var currentAdmin = await _userManager.GetUserAsync(User);
        var currentAdminId = currentAdmin?.Id ?? string.Empty;

        var result = await _accountService.UpdateAccountAsync(currentAdminId, userId, fullName, email, phone, address, role);
        AlertMessage = result.Message;
        AlertType = result.Success ? "success" : (result.Message.Contains("hạ quyền") ? "warning" : "danger");
        return RedirectToPage(new { SearchKeyword });
    }

    /// <summary>
    /// Xóa tài khoản qua AccountService (tự động khóa nếu có đơn hàng)
    /// </summary>
    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        var currentAdmin = await _userManager.GetUserAsync(User);
        var currentAdminId = currentAdmin?.Id ?? string.Empty;

        var result = await _accountService.DeleteAccountAsync(currentAdminId, userId);
        AlertMessage = result.Message;
        AlertType = result.Message.Contains("KHÓA VĨNH VIỄN") ? "warning" : (result.Success ? "success" : "danger");
        return RedirectToPage(new { SearchKeyword });
    }

    /// <summary>
    /// Khóa hoặc Mở khóa tài khoản người dùng
    /// </summary>
    public async Task<IActionResult> OnPostToggleActiveAsync(string userId)
    {
        var currentAdmin = await _userManager.GetUserAsync(User);
        var currentAdminId = currentAdmin?.Id ?? string.Empty;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return RedirectToPage(new { SearchKeyword });

        if (user.Id == currentAdminId)
        {
            AlertMessage = "Bạn không thể tự khóa tài khoản Admin của chính mình!";
            AlertType = "warning";
            return RedirectToPage(new { SearchKeyword });
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        AlertMessage = user.IsActive 
            ? $"Đã MỞ KHÓA tài khoản '{user.UserName}' thành công!" 
            : $"Đã KHÓA tài khoản '{user.UserName}'! Người này sẽ không thể đăng nhập.";
        AlertType = user.IsActive ? "success" : "danger";

        return RedirectToPage(new { SearchKeyword });
    }
}
