using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ElectronicStore_blazor.Services;

/// <summary>
/// Đồng bộ trạng thái xác thực của UserService sang AuthenticationStateProvider chuẩn của Blazor Server
/// </summary>
public class BlazorAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly UserService _userService;

    public BlazorAuthStateProvider(UserService userService)
    {
        _userService = userService;
        _userService.OnChange += HandleUserChanged;
    }

    private void HandleUserChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_userService.IsAuthenticated && _userService.CurrentUser != null)
        {
            var user = _userService.CurrentUser;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Role, string.IsNullOrEmpty(_userService.CurrentRole) ? "Customer" : _userService.CurrentRole)
            };

            var identity = new ClaimsIdentity(claims, "BlazorCustomAuth");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    public void Dispose()
    {
        _userService.OnChange -= HandleUserChanged;
    }
}

