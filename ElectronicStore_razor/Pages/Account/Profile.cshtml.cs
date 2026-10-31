using System.ComponentModel.DataAnnotations;
using ElectronicStore_razor.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectronicStore_razor.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public ProfileInputModel Input { get; set; } = new();

    public string UserName { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class ProfileInputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ nhận hàng")]
        public string? Address { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không trùng khớp")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string? ConfirmPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Lấy thông tin user hiện tại từ Identity
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login");
        }

        UserName = user.UserName ?? "";
        Input = new ProfileInputModel
        {
            FullName = user.FullName ?? "",
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            Address = user.Address == "Chưa cập nhật địa chỉ" ? "" : user.Address
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login");
        }

        UserName = user.UserName ?? "";

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Cập nhật thông tin cá nhân
        user.FullName = Input.FullName.Trim();
        user.Email = Input.Email.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(Input.Address) || Input.Address.Trim() == "Chưa cập nhật địa chỉ" ? null : Input.Address.Trim();

        // Xử lý đổi mật khẩu nếu người dùng nhập mật khẩu mới
        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(Input.CurrentPassword))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu hiện tại để đổi sang mật khẩu mới!";
                return Page();
            }

            var changePassResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
            if (!changePassResult.Succeeded)
            {
                ErrorMessage = string.Join("; ", changePassResult.Errors.Select(e => e.Description));
                return Page();
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (updateResult.Succeeded)
        {
            // Làm mới cookie xác thực nếu có thay đổi
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Cập nhật thông tin tài khoản thành công!";
            return RedirectToPage();
        }

        ErrorMessage = string.Join("; ", updateResult.Errors.Select(e => e.Description));
        return Page();
    }
}
