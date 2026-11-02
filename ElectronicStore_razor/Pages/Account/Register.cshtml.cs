using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;

namespace ElectronicStore_razor.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(
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

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string PhoneNumber { get; set; } = string.Empty;

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string Address { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || 
            string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(PhoneNumber))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ các trường bắt buộc!";
            return Page();
        }

        var actualEmail = string.IsNullOrWhiteSpace(Email) ? $"{Username.Trim()}@electrostore.vn" : Email.Trim();
        var actualAddress = string.IsNullOrWhiteSpace(Address) ? "Chưa cập nhật địa chỉ" : Address.Trim();

        var result = await _accountService.RegisterAsync(
            Username, FullName, actualEmail, PhoneNumber, actualAddress, Password);

        if (result.Success && result.User != null)
        {
            await _signInManager.SignInAsync(result.User, isPersistent: true);
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }
            return RedirectToPage("/Index");
        }

        ErrorMessage = result.Message;
        return Page();
    }
}
