using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages.Account;

public class LoginModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
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
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
            return Page();
        }

        // Tìm tài khoản theo Username chuẩn Identity
        var user = await _userManager.FindByNameAsync(Username.Trim());
        if (user == null)
        {
            ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return Page();
        }

        // KIỂM TRA TRẠNG THÁI KHÓA TÀI KHOẢN
        if (!user.IsActive)
        {
            ErrorMessage = "Tài khoản của bạn đã bị Admin KHÓA! Vui lòng liên hệ ban quản trị để được hỗ trợ.";
            return Page();
        }

        // Xác thực mật khẩu bằng SignInManager chuẩn Identity
        var result = await _signInManager.PasswordSignInAsync(user, Password.Trim(), isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            // Nếu người dùng có quyền Admin thì có thể chuyển thẳng đến trang quản trị
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/Admin/Products");
            }

            return RedirectToPage("/Index");
        }

        ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
        return Page();
    }
}
