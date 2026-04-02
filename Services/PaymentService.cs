using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace ChatPortal.Services;

public interface IPaymentService
{
    Task<string> CreateStripeCheckoutSessionAsync(int userId, int packageId, string successUrl, string cancelUrl);
    Task<bool> HandleStripeWebhookAsync(string json, string stripeSignature);
    Task<string> CreatePayPalOrderAsync(int userId, int packageId);
    Task<bool> CapturePayPalOrderAsync(int userId, string orderId);
    Task<List<CreditPackage>> GetActiveCreditPackagesAsync();
    Task<List<PaymentTransaction>> GetUserPaymentHistoryAsync(int userId);
}

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly ICreditService _creditService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext db, ICreditService creditService, IConfiguration configuration, ILogger<PaymentService> logger)
    {
        _db = db;
        _creditService = creditService;
        _configuration = configuration;
        _logger = logger;

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<List<CreditPackage>> GetActiveCreditPackagesAsync()
    {
        return await _db.CreditPackages
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public async Task<List<PaymentTransaction>> GetUserPaymentHistoryAsync(int userId)
    {
        return await _db.PaymentTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .ToListAsync();
    }

    public async Task<string> CreateStripeCheckoutSessionAsync(int userId, int packageId, string successUrl, string cancelUrl)
    {
        var package = await _db.CreditPackages.FindAsync(packageId)
            ?? throw new KeyNotFoundException("Credit package not found.");

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(package.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{package.Name} — {package.Credits} Credits",
                            Description = package.Description
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "packageId", packageId.ToString() }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        // Record pending transaction
        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId = userId,
            CreditPackageId = packageId,
            Amount = package.Price,
            Provider = "Stripe",
            Status = "Pending",
            ProviderSessionId = session.Id
        });
        await _db.SaveChangesAsync();

        return session.Url;
    }

    public async Task<bool> HandleStripeWebhookAsync(string json, string stripeSignature)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"] ?? string.Empty;
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                var session = (Session)stripeEvent.Data.Object;
                await FulfillStripeOrderAsync(session);
            }
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook validation failed");
            return false;
        }
    }

    private async Task FulfillStripeOrderAsync(Session session)
    {
        var tx = await _db.PaymentTransactions
            .FirstOrDefaultAsync(t => t.ProviderSessionId == session.Id);

        if (tx == null || tx.Status == "Completed")
            return;

        tx.Status = "Completed";
        tx.ProviderTransactionId = session.PaymentIntentId;
        tx.UpdatedAt = DateTime.UtcNow;

        var package = await _db.CreditPackages.FindAsync(tx.CreditPackageId);
        if (package != null)
        {
            tx.CreditsAwarded = package.Credits;
            await _creditService.AddCreditsAsync(tx.UserId, package.Credits, "Purchase",
                $"Purchased {package.Name} package via Stripe ({package.Credits} credits)");
        }

        await _db.SaveChangesAsync();
    }

    public async Task<string> CreatePayPalOrderAsync(int userId, int packageId)
    {
        var package = await _db.CreditPackages.FindAsync(packageId)
            ?? throw new KeyNotFoundException("Credit package not found.");

        // Record pending transaction
        var tx = new PaymentTransaction
        {
            UserId = userId,
            CreditPackageId = packageId,
            Amount = package.Price,
            Provider = "PayPal",
            Status = "Pending"
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        // Return the transaction ID for frontend approval flow
        return tx.Id.ToString();
    }

    public async Task<bool> CapturePayPalOrderAsync(int userId, string orderId)
    {
        if (!int.TryParse(orderId, out var txId))
            return false;

        var tx = await _db.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == txId && t.UserId == userId && t.Provider == "PayPal");

        if (tx == null || tx.Status == "Completed")
            return false;

        tx.Status = "Completed";
        tx.ProviderTransactionId = orderId;
        tx.UpdatedAt = DateTime.UtcNow;

        var package = await _db.CreditPackages.FindAsync(tx.CreditPackageId);
        if (package != null)
        {
            tx.CreditsAwarded = package.Credits;
            await _creditService.AddCreditsAsync(tx.UserId, package.Credits, "Purchase",
                $"Purchased {package.Name} package via PayPal ({package.Credits} credits)");
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
