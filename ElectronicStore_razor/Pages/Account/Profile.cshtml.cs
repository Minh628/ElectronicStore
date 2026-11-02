using System.ComponentModel.DataAnnotations;
using ElectronicStore_razor.Models.Entities;
using ElectronicStore_razor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ElectronicStore_razor.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileModel(
        IAccountService accountService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _accountService = accountService;
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

        // Cập nhật thông tin cá nhân qua AccountService
        var updateProfile = await _accountService.UpdateProfileAsync(
            user.Id, Input.FullName, Input.PhoneNumber ?? "", Input.Address ?? "");

        if (!updateProfile.Success)
        {
            ErrorMessage = updateProfile.Message;
            return Page();
        }

        // Xử lý đổi mật khẩu nếu có qua AccountService
        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(Input.CurrentPassword))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu hiện tại để đổi sang mật khẩu mới!";
                return Page();
            }

            var changePass = await _accountService.ChangePasswordAsync(user.Id, Input.CurrentPassword, Input.NewPassword);
            if (!changePass.Success)
            {
                ErrorMessage = changePass.Message;
                return Page();
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Cập nhật thông tin tài khoản thành công!";
        return RedirectToPage();
    }
}
