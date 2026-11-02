using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services.Admin;

namespace ElectronicStore_razor.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AccountsModel : PageModel
{
    private readonly IAdminAccountService _accountService;

    public AccountsModel(IAdminAccountService accountService)
    {
        _accountService = accountService;
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
    /// Thêm tài khoản người dùng mới qua AdminAccountService
    /// </summary>
    public async Task<IActionResult> OnPostCreateUserAsync(string username, string fullName, string email, string phone, string address, string password, string role)
    {
        var result = await _accountService.CreateAccountAsync(username, fullName, email, phone, address, password, role);
        AlertMessage = result.Message;
        AlertType = result.Success ? "success" : "danger";
        return RedirectToPage(new { SearchKeyword });
    }

    /// <summary>
    /// Cập nhật thông tin và vai trò của tài khoản qua AdminAccountService
    /// </summary>
    public async Task<IActionResult> OnPostUpdateUserAsync(string userId, string fullName, string email, string phone, string address, string role)
    {
        var currentAdminId = _accountService.GetCurrentAdminId(User);

        var result = await _accountService.UpdateAccountAsync(currentAdminId, userId, fullName, email, phone, address, role);
        AlertMessage = result.Message;
        AlertType = result.Success ? "success" : (result.Message.Contains("hạ quyền") ? "warning" : "danger");
        return RedirectToPage(new { SearchKeyword });
    }

    /// <summary>
    /// Xóa tài khoản qua AdminAccountService (tự động khóa nếu có đơn hàng)
    /// </summary>
    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        var currentAdminId = _accountService.GetCurrentAdminId(User);

        var result = await _accountService.DeleteAccountAsync(currentAdminId, userId);
        AlertMessage = result.Message;
        AlertType = result.Message.Contains("KHÓA VĨNH VIỄN") ? "warning" : (result.Success ? "success" : "danger");
        return RedirectToPage(new { SearchKeyword });
    }

    /// <summary>
    /// Khóa hoặc Mở khóa tài khoản người dùng qua AdminAccountService
    /// </summary>
    public async Task<IActionResult> OnPostToggleActiveAsync(string userId)
    {
        var currentAdminId = _accountService.GetCurrentAdminId(User);

        var result = await _accountService.ToggleAccountLockAsync(currentAdminId, userId);
        AlertMessage = result.Message;
        AlertType = result.Success ? (result.Message.Contains("MỞ KHÓA") ? "success" : "danger") : "warning";

        return RedirectToPage(new { SearchKeyword });
    }
}
