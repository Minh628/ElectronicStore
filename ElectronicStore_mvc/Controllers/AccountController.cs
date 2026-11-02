using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ElectronicStore_mvc.Models.Entities;
using ElectronicStore_mvc.Services;

namespace ElectronicStore_mvc.Controllers;

/// <summary>
/// Controller xử lý xác thực, đăng nhập/đăng ký và hồ sơ người dùng (Thin Controller)
/// </summary>
public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly ElectronicStore_mvc.Areas.Admin.Services.IAdminAccountService _adminAccountService;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        IAccountService accountService,
        ElectronicStore_mvc.Areas.Admin.Services.IAdminAccountService adminAccountService,
        SignInManager<ApplicationUser> signInManager)
    {
        _accountService = accountService;
        _adminAccountService = adminAccountService;
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
    /// Xử lý xác thực đăng nhập Cổng Quản Trị Viên thông qua AccountService
    /// </summary>
    [HttpPost]
    [Route("admin/login")]
    public async Task<IActionResult> AdminLogin(string username, string password, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;

        var validation = await _adminAccountService.ValidateAdminLoginAsync(username, password);
        if (!validation.Success || validation.User == null)
        {
            ViewBag.ErrorMessage = validation.Message;
            return View();
        }

        var user = validation.User;
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

    /// <summary>
    /// Xử lý xác thực đăng nhập Cổng Khách Hàng (chặn tài khoản Admin để bảo mật phân tách cổng)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;

        var validation = await _accountService.ValidateCustomerLoginAsync(username, password);
        if (!validation.Success || validation.User == null)
        {
            ViewBag.ErrorMessage = validation.Message;
            return View();
        }

        var user = validation.User;
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
    public async Task<IActionResult> Register(
        string username, string password, string fullName, string? email, string? phoneNumber, string? phone, string? address, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        var actualPhone = !string.IsNullOrWhiteSpace(phoneNumber) ? phoneNumber : phone;
        var actualEmail = string.IsNullOrWhiteSpace(email) ? $"{username.Trim()}@electrostore.vn" : email.Trim();

        var regResult = await _accountService.RegisterAsync(
            username, fullName, actualEmail, actualPhone ?? "0900000000", address ?? "Chưa cập nhật địa chỉ", password);

        if (regResult.Success && regResult.User != null)
        {
            await _signInManager.SignInAsync(regResult.User, isPersistent: true);

            // Điều hướng quay lại URL ban đầu nếu có
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ErrorMessage = regResult.Message;
        return View();
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _accountService.GetCurrentUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(user);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost]
    public async Task<IActionResult> Profile(
        string fullName, string email, string? phoneNumber, string? address, string? currentPassword, string? newPassword, string? confirmPassword)
    {
        var user = await _accountService.GetCurrentUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ họ tên và email!";
            return View(user);
        }

        // Cập nhật thông tin qua AccountService
        var updateProfile = await _accountService.UpdateProfileAsync(user.Id, fullName, phoneNumber ?? "", address ?? "");
        if (!updateProfile.Success)
        {
            ViewBag.ErrorMessage = updateProfile.Message;
            return View(user);
        }

        // Đổi mật khẩu nếu có
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

            var changePass = await _accountService.ChangePasswordAsync(user.Id, currentPassword, newPassword);
            if (!changePass.Success)
            {
                ViewBag.ErrorMessage = changePass.Message;
                return View(user);
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        ViewBag.SuccessMessage = "Cập nhật thông tin tài khoản thành công!";
        return View(user);
    }

    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Remove("LastGuestOrderId");
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Đăng xuất quản trị viên và chuyển hướng về Cổng Đăng Nhập Quản Trị
    /// </summary>
    public async Task<IActionResult> AdminLogout()
    {
        HttpContext.Session.Remove("LastGuestOrderId");
        await _signInManager.SignOutAsync();
        return RedirectToAction("AdminLogin", "Account");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
