using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(
        IAccountService accountService,
        SignInManager<ApplicationUser> signInManager)
    {
        _accountService = accountService;
        _signInManager = signInManager;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var validation = await _accountService.ValidateCustomerLoginAsync(Username, Password);
        if (!validation.Success || validation.User == null)
        {
            ErrorMessage = validation.Message;
            return Page();
        }

        var user = validation.User;
        if (!user.IsActive)
        {
            ErrorMessage = "Tài khoản của bạn đã bị Admin KHÓA! Vui lòng liên hệ ban quản trị để được hỗ trợ.";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user, Password.Trim(), isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }

        ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
        return Page();
    }
}
