using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VbeSort.Data;
using VbeSort.Infrastructure;
using VbeSort.Models;

namespace VbeSort.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public AccountController(AppDbContext context) => _context = context;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalized = model.Username.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == normalized);

        if (user is not null &&
            _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) != PasswordVerificationResult.Failed)
        {
            await SignInUserAsync(user, model.RememberMe);
            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Neteisingas prisijungimo vardas arba slaptažodis.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = model.Username.Trim();
        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        {
            ModelState.AddModelError(nameof(model.Username), "Toks prisijungimo vardas jau užimtas.");
            return View(model);
        }

        var user = new User
        {
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? username : model.DisplayName.Trim(),
            Role = "Student",
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, model.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await SignInUserAsync(user, isPersistent: true);
        TempData["Success"] = $"Sveiki atvykę, {user.EffectiveDisplayName}! Paskyra sukurta.";
        return RedirectToLocal(returnUrl);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    internal async Task SignInUserAsync(User user, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(AppClaimTypes.DisplayName, user.EffectiveDisplayName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var props = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            props);
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
