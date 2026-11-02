using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages.Admin;

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

    public IActionResult OnGet()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin"))
        {
            return RedirectToPage("/Admin/Products");
        }

        return Page();
    }

    public async Task<IActionResult> OnGetLogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Admin/Login");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var validation = await _accountService.ValidateAdminLoginAsync(Username, Password);
        if (!validation.Success || validation.User == null)
        {
            ErrorMessage = validation.Message;
            return Page();
        }

        var user = validation.User;
        if (!user.IsActive)
        {
            ErrorMessage = "Tài khoản quản trị viên này hiện đang bị tạm khóa!";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user, Password.Trim(), isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Admin/Products");
        }

        ErrorMessage = "Tài khoản hoặc mật khẩu quản trị không chính xác!";
        return Page();
    }
}
