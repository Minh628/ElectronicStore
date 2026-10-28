using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages.Admin;

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

    public IActionResult OnGet()
    {
        // Nếu đã đăng nhập quyền Admin thì chuyển thẳng vào Bảng Quản Trị
        if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin"))
        {
            return RedirectToPage("/Admin/Products");
        }

        return Page();
    }

    /// <summary>
    /// Đăng xuất quản trị viên và chuyển hướng về Cổng Đăng Nhập Quản Trị
    /// </summary>
    public async Task<IActionResult> OnGetLogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Admin/Login");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu quản trị!";
            return Page();
        }

        // Tìm tài khoản quản trị
        var user = await _userManager.FindByNameAsync(Username.Trim());
        if (user == null)
        {
            ErrorMessage = "Tài khoản hoặc mật khẩu quản trị không chính xác!";
            return Page();
        }

        // KIỂM TRA ROLE ADMIN: Chặn hoàn toàn tài khoản khách hàng thông thường
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            ErrorMessage = "Truy cập bị từ chối: Tài khoản của bạn không có quyền truy cập vào Cổng Quản Trị Hệ Thống!";
            return Page();
        }

        if (!user.IsActive)
        {
            ErrorMessage = "Tài khoản quản trị viên này hiện đang bị tạm khóa!";
            return Page();
        }

        // Xác thực mật khẩu
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
