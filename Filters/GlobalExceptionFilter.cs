using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ChatPortal.Filters;

/// <summary>
/// A global exception filter that intercepts unhandled exceptions thrown by any
/// controller action and surfaces a user-friendly error message rather than
/// exposing raw exception details to the client.
/// </summary>
/// <remarks>
/// For AJAX / JSON requests the filter returns a JSON object with an <c>error</c>
/// and <c>requestId</c> property.  For regular page requests it sets
/// <c>TempData["Error"]</c> and <c>TempData["RequestId"]</c> and redirects to
/// the home page so the user sees the standard error banner.
/// </remarks>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initialises a new instance of <see cref="GlobalExceptionFilter"/>.
    /// </summary>
    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger,
        ITempDataDictionaryFactory tempDataFactory,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _tempDataFactory = tempDataFactory;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public void OnException(ExceptionContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionName = context.RouteData.Values["action"]?.ToString();

        _logger.LogError(context.Exception,
            "Unhandled exception [RequestId={RequestId}] in {Controller}.{Action}",
            requestId,
            controllerName,
            actionName);

        var friendlyMessage = GetUserFriendlyMessage(context.Exception);

        // Persist error to database
        TrySaveErrorLog(context, requestId, controllerName, actionName, friendlyMessage);

        var isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                     || (context.HttpContext.Request.ContentType?.Contains("application/json") == true
                         && context.HttpContext.Request.Method != HttpMethods.Get);

        if (isAjax)
        {
            context.Result = new ObjectResult(new { error = friendlyMessage, requestId })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        else
        {
            var tempData = _tempDataFactory.GetTempData(context.HttpContext);
            tempData["Error"] = friendlyMessage;
            tempData["RequestId"] = requestId;

            context.Result = new RedirectToActionResult("Index", "Home", null);
        }

        context.ExceptionHandled = true;
    }

    private void TrySaveErrorLog(ExceptionContext context, string requestId,
        string? controllerName, string? actionName, string friendlyMessage)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Try to get organisation name from session
            string? orgName = null;
            try
            {
                var orgId = context.HttpContext.Session.GetInt32("ActiveOrganizationId");
                if (orgId.HasValue)
                    orgName = db.Organizations.Where(o => o.Id == orgId.Value).Select(o => o.Name).FirstOrDefault();
            }
            catch { /* session may not be available */ }

            // Try to get user id from claims
            int? userId = null;
            var userIdClaim = context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var uid))
                userId = uid;

            var fullMessage = $"{context.Exception.Message}\n\n{context.Exception.StackTrace}";

            var errorLog = new ErrorLog
            {
                RequestId = requestId,
                ControllerName = controllerName,
                ActionName = actionName,
                OrganizationName = orgName,
                ErrorMessage = fullMessage,
                UserFriendlyMessage = friendlyMessage,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                RequestPath = context.HttpContext.Request.Path,
                HttpMethod = context.HttpContext.Request.Method
            };

            db.ErrorLogs.Add(errorLog);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist error log for RequestId={RequestId}", requestId);
        }
    }

    /// <summary>
    /// Maps well-known exception types to concise, user-friendly messages.
    /// Falls back to a generic message for unexpected exception types.
    /// </summary>
    private static string GetUserFriendlyMessage(Exception ex) => ex switch
    {
        InvalidOperationException => ex.Message,
        UnauthorizedAccessException => "You do not have permission to perform this action.",
        KeyNotFoundException => "The requested resource was not found.",
        TimeoutException => "The request timed out. Please try again.",
        HttpRequestException => "A network error occurred while contacting an external service. Please try again.",
        _ => "An unexpected error occurred. Please try again or contact support if the problem persists."
    };
}
