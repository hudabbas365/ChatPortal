using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace ChatPortal.Services;

public interface IPaymentService
{
    Task<string> CreateStripeSubscriptionAsync(Guid orgId, Guid planId, string successUrl, string cancelUrl);
    Task<bool> HandleStripeWebhookAsync(string json, string stripeSignature);
    Task<string> CreatePayPalSubscriptionAsync(Guid orgId, Guid planId);
    Task<bool> CapturePayPalOrderAsync(Guid userId, string orderId);
    Task HandleFailedPaymentAsync(Guid orgId);
    Task<bool> RetryPaymentAsync(Guid transactionId);
    Task<List<PaymentTransaction>> GetPaymentHistoryAsync(Guid orgId);
}

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(AppDbContext db, IConfiguration configuration, ILogger<PaymentService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<List<PaymentTransaction>> GetPaymentHistoryAsync(Guid orgId)
    {
        // Get users in org
        var userIds = await _db.OrganizationMembers
            .Where(m => m.OrganizationId == orgId)
            .Select(m => m.UserId)
            .ToListAsync();

        return await _db.PaymentTransactions
            .Where(t => userIds.Contains(t.UserId))
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<string> CreateStripeSubscriptionAsync(Guid orgId, Guid planId, string successUrl, string cancelUrl)
    {
        var plan = await _db.Plans.FindAsync(planId)
            ?? throw new KeyNotFoundException("Plan not found.");

        var org = await _db.Organizations.FindAsync(orgId)
            ?? throw new KeyNotFoundException("Organization not found.");

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(plan.MonthlyPrice * 100),
                        Currency = "usd",
                        Recurring = new SessionLineItemPriceDataRecurringOptions { Interval = "month" },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{plan.Name} Plan",
                            Description = plan.Description
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "subscription",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "orgId", orgId.ToString() },
                { "planId", planId.ToString() }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        _db.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId = org.OwnerId,
            PlanId = planId,
            Amount = plan.MonthlyPrice,
            Provider = "Stripe",
            Status = "Pending",
            ProviderSessionId = session.Id,
            Description = $"{plan.Name} Plan subscription"
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

        // Activate subscription if plan was set
        if (tx.PlanId.HasValue && session.Metadata.TryGetValue("orgId", out var orgIdStr) && Guid.TryParse(orgIdStr, out var orgId))
        {
            var existingSub = await _db.Subscriptions
                .Where(s => s.OrganizationId == orgId && s.Status == "Active")
                .FirstOrDefaultAsync();

            if (existingSub != null)
            {
                existingSub.PlanId = tx.PlanId.Value;
                existingSub.Status = "Active";
                existingSub.StartDate = DateTime.UtcNow;
                existingSub.EndDate = DateTime.UtcNow.AddMonths(1);
            }
            else
            {
                _db.Subscriptions.Add(new ChatPortal.Models.Entities.Subscription
                {
                    UserId = tx.UserId,
                    OrganizationId = orgId,
                    PlanId = tx.PlanId.Value,
                    Status = "Active",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1)
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<string> CreatePayPalSubscriptionAsync(Guid orgId, Guid planId)
    {
        var plan = await _db.Plans.FindAsync(planId)
            ?? throw new KeyNotFoundException("Plan not found.");

        var org = await _db.Organizations.FindAsync(orgId)
            ?? throw new KeyNotFoundException("Organization not found.");

        var tx = new PaymentTransaction
        {
            UserId = org.OwnerId,
            PlanId = planId,
            Amount = plan.MonthlyPrice,
            Provider = "PayPal",
            Status = "Pending",
            Description = $"{plan.Name} Plan subscription"
        };
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();

        return tx.Id.ToString();
    }

    public async Task<bool> CapturePayPalOrderAsync(Guid userId, string orderId)
    {
        if (!Guid.TryParse(orderId, out var txId))
            return false;

        var tx = await _db.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == txId && t.UserId == userId && t.Provider == "PayPal");

        if (tx == null || tx.Status == "Completed")
            return false;

        tx.Status = "Completed";
        tx.ProviderTransactionId = orderId;
        tx.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task HandleFailedPaymentAsync(Guid orgId)
    {
        _logger.LogWarning("Payment failed for organization {OrgId}", orgId);
        // Log failed payment and could trigger notification
    }

    public async Task<bool> RetryPaymentAsync(Guid transactionId)
    {
        var tx = await _db.PaymentTransactions.FindAsync(transactionId);
        if (tx == null || tx.Status != "Failed")
            return false;

        tx.Status = "Pending";
        tx.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
