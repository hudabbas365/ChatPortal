using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService)
    {
        _userService = userService;
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
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Settings(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        TempData["Success"] = "Profile updated successfully.";
        return View(model);
    }
}
