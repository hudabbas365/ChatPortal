using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AccountController> _logger;
    private readonly ICreditService _creditService;

    public AccountController(IUserService userService, IJwtService jwtService, ILogger<AccountController> logger, ICreditService creditService)
    {
        _userService = userService;
        _jwtService = jwtService;
        _logger = logger;
        _creditService = creditService;
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
    /// Redirects to the Microsoft Identity Platform for Office 365 authentication.
    /// This is a stub that shows a not-yet-configured message until MSAL is integrated.
    /// </summary>
    [HttpGet]
    public IActionResult LoginMicrosoft(string? returnUrl = null)
    {
        // When Microsoft Identity / MSAL is configured, this action would
        // use Challenge("AzureAD") to initiate the OAuth2 flow.
        // For now we surface a clear message so users understand the intent.
        TempData["Error"] = "Microsoft / Office 365 sign-in is not yet configured for this environment. Please use email and password to sign in.";
        return RedirectToAction("Login", new { returnUrl });
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embed token for user {UserId}", userId);
            return StatusCode(500, new { success = false, error = "Failed to generate embed token. Please try again." });
        }
    }

    // GET: Account/GetCreditsBalance
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetCreditsBalance()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return Json(new { balance = 0 });

        var balance = await _creditService.GetBalanceAsync(userId);
        return Json(new { balance });
    }

    // GET: Account/Credits
    [Authorize]
    public async Task<IActionResult> Credits()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return RedirectToAction("Login");

        var balance = await _creditService.GetBalanceAsync(userId);
        ViewBag.CreditsBalance = balance;
        return View();
    }
}
