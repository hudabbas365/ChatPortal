using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;

    public AccountController(IUserService userService, IJwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.LoginAsync(model.Email, model.Password);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Login failed.");
            return View(model);
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : DateTimeOffset.UtcNow.AddHours(1)
        };
        Response.Cookies.Append("access_token", result.AccessToken!, cookieOptions);

        var returnUrl = model.ReturnUrl;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.RegisterAsync(
            model.FirstName, model.LastName, model.Email, model.Password);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Registration failed.");
            return View(model);
        }

        TempData["Success"] = "Registration successful! Please log in.";
        return RedirectToAction("Login");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Settings()
    {
        var vm = new ProfileViewModel
        {
            FirstName = "Demo",
            LastName = "User",
            Email = "demo@chatportal.com"
        };
        ViewBag.IsAgent = User.IsInRole("Agent");
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Settings(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        TempData["Success"] = "Profile updated successfully.";
        return View(model);
    }

    /// <summary>
    /// Generates a short-lived JWT embed token for the authenticated user so they
    /// can publish their chat interface as an iframe on external websites.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GenerateEmbedToken()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { success = false, error = "You must be logged in to generate an embed token." });

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";

        if (!int.TryParse(userIdStr, out var userId))
            return BadRequest(new { success = false, error = "Invalid user identity." });

        try
        {
            var token = _jwtService.GenerateAccessToken(userId, email, role);
            return Json(new { success = true, token });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, error = "Failed to generate embed token. Please try again." });
        }
    }
}
