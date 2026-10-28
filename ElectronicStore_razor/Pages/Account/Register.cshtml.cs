using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ElectronicStore_razor.Models.Entities;

namespace ElectronicStore_razor.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(
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

        // Kiểm tra xem Username đã tồn tại chưa
        var existingUser = await _userManager.FindByNameAsync(Username.Trim());
        if (existingUser != null)
        {
            ErrorMessage = "Tên đăng nhập này đã được sử dụng! Vui lòng chọn tên khác.";
            return Page();
        }

        // Tạo đối tượng ApplicationUser chuẩn Identity (lưu trữ đầy đủ thông tin cá nhân)
        var user = new ApplicationUser
        {
            UserName = Username.Trim(),
            FullName = FullName.Trim(),
            PhoneNumber = PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(Email) ? $"{Username.Trim()}@electrostore.vn" : Email.Trim(),
            Address = string.IsNullOrWhiteSpace(Address) ? "Chưa cập nhật địa chỉ" : Address.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, Password.Trim());
        if (result.Succeeded)
        {
            // Gán role 'Customer'
            await _userManager.AddToRoleAsync(user, "Customer");

            // Đăng nhập tự động
            await _signInManager.SignInAsync(user, isPersistent: true);

            // Điều hướng quay lại trang thanh toán hoặc url ban đầu nếu có
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }

        ErrorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
        return Page();
    }
}
