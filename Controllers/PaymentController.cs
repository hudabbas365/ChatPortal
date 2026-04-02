using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ICreditService _creditService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ICreditService creditService,
        IConfiguration configuration, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _creditService = creditService;
        _configuration = configuration;
        _logger = logger;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> SelectPlan()
    {
        var userId = GetUserId();
        var vm = new PaymentViewModel
        {
            Packages = await _paymentService.GetActiveCreditPackagesAsync(),
            CurrentBalance = await _creditService.GetBalanceAsync(userId),
            RecentTransactions = await _paymentService.GetUserPaymentHistoryAsync(userId),
            PublishableKey = _configuration["Stripe:PublishableKey"],
            PayPalClientId = _configuration["PayPal:ClientId"]
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessStripePayment(int packageId)
    {
        try
        {
            var userId = GetUserId();
            var successUrl = Url.Action("Success", "Payment", null, Request.Scheme)!;
            var cancelUrl = Url.Action("Cancel", "Payment", null, Request.Scheme)!;
            var checkoutUrl = await _paymentService.CreateStripeCheckoutSessionAsync(userId, packageId, successUrl, cancelUrl);
            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe checkout creation failed");
            TempData["Error"] = "Failed to initiate payment. Please try again.";
            return RedirectToAction(nameof(SelectPlan));
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var success = await _paymentService.HandleStripeWebhookAsync(json, signature);
        return success ? Ok() : BadRequest();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayPalPayment(int packageId)
    {
        try
        {
            var userId = GetUserId();
            var orderId = await _paymentService.CreatePayPalOrderAsync(userId, packageId);
            return Json(new { success = true, orderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayPal order creation failed");
            return Json(new { success = false, error = "Failed to create PayPal order." });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapturePayPalPayment(string orderId)
    {
        var userId = GetUserId();
        var captured = await _paymentService.CapturePayPalOrderAsync(userId, orderId);
        if (captured)
            return RedirectToAction(nameof(Success));

        TempData["Error"] = "PayPal payment capture failed. Please contact support.";
        return RedirectToAction(nameof(Cancel));
    }

    public IActionResult Success()
    {
        return View();
    }

    public IActionResult Cancel()
    {
        return View();
    }
}
