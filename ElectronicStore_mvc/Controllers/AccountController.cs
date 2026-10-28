using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ElectronicStore_mvc.Models.Entities;

namespace ElectronicStore_mvc.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// Cổng Đăng Nhập Dành Riêng Cho Quản Trị Viên (Admin Portal)
    /// </summary>
    [HttpGet]
    [Route("admin/login")]
    public IActionResult AdminLogin(string? returnUrl)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin"))
        {
            return RedirectToAction("Products", "Admin", new { area = "Admin" });
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// Xử lý xác thực đăng nhập Cổng Quản Trị Viên
    /// </summary>
    [HttpPost]
    [Route("admin/login")]
    public async Task<IActionResult> AdminLogin(string username, string password, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu quản trị!";
            return View();
        }

        var user = await _userManager.FindByNameAsync(username.Trim());
        if (user == null)
        {
            ViewBag.ErrorMessage = "Tài khoản hoặc mật khẩu quản trị không chính xác!";
            return View();
        }

        // KIỂM TRA ROLE ADMIN: Chặn hoàn toàn tài khoản khách hàng thông thường
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            ViewBag.ErrorMessage = "Truy cập bị từ chối: Tài khoản của bạn không có quyền truy cập vào Cổng Quản Trị Hệ Thống!";
            return View();
        }

        if (!user.IsActive)
        {
            ViewBag.ErrorMessage = "Tài khoản quản trị viên này hiện đang bị tạm khóa!";
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(user, password.Trim(), isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Products", "Admin", new { area = "Admin" });
        }

        ViewBag.ErrorMessage = "Tài khoản hoặc mật khẩu quản trị không chính xác!";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
            return View();
        }

        var user = await _userManager.FindByNameAsync(username.Trim());
        if (user == null)
        {
            ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return View();
        }

        if (!user.IsActive)
        {
            ViewBag.ErrorMessage = "Tài khoản của bạn đã bị Admin KHÓA! Vui lòng liên hệ ban quản trị.";
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(user, password.Trim(), isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Products", "Admin", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
        return View();
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string username, string password, string fullName, string? email, string? phoneNumber, string? phone, string? address, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        var actualPhone = !string.IsNullOrWhiteSpace(phoneNumber) ? phoneNumber : phone;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
        {
            ViewBag.ErrorMessage = "Vui lòng điền đầy đủ các thông tin bắt buộc!";
            return View();
        }

        var existingUser = await _userManager.FindByNameAsync(username.Trim());
        if (existingUser != null)
        {
            ViewBag.ErrorMessage = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!";
            return View();
        }

        // Tạo ApplicationUser kế thừa IdentityUser chuẩn
        var user = new ApplicationUser
        {
            UserName = username.Trim(),
            FullName = fullName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? $"{username.Trim()}@electrostore.vn" : email.Trim(),
            PhoneNumber = actualPhone?.Trim() ?? "0900000000",
            Address = string.IsNullOrWhiteSpace(address) ? "Chưa cập nhật địa chỉ" : address.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var createResult = await _userManager.CreateAsync(user, password.Trim());
        if (createResult.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");
            await _signInManager.SignInAsync(user, isPersistent: true);

            // Điều hướng quay lại trang thanh toán hoặc url ban đầu nếu có
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ErrorMessage = string.Join("; ", createResult.Errors.Select(e => e.Description));
        return View();
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(user);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost]
    public async Task<IActionResult> Profile(string fullName, string email, string? phoneNumber, string? address, string? currentPassword, string? newPassword, string? confirmPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ họ tên và email!";
            return View(user);
        }

        // Cập nhật thông tin cá nhân
        user.FullName = fullName.Trim();
        user.Email = email.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        user.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

        // Xử lý đổi mật khẩu nếu có
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập mật khẩu hiện tại để đổi sang mật khẩu mới!";
                return View(user);
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không trùng khớp!";
                return View(user);
            }

            var changePassResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!changePassResult.Succeeded)
            {
                ViewBag.ErrorMessage = string.Join("; ", changePassResult.Errors.Select(e => e.Description));
                return View(user);
            }
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (updateResult.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            ViewBag.SuccessMessage = "Cập nhật thông tin tài khoản thành công!";
            return View(user);
        }

        ViewBag.ErrorMessage = string.Join("; ", updateResult.Errors.Select(e => e.Description));
        return View(user);
    }

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Đăng xuất quản trị viên và chuyển hướng về Cổng Đăng Nhập Quản Trị
    /// </summary>
    public async Task<IActionResult> AdminLogout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("AdminLogin", "Account");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
