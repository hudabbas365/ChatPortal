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
/// property.  For regular page requests it sets <c>TempData["Error"]</c> and
/// redirects to the home page so the user sees the standard error banner.
/// </remarks>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    /// <summary>
    /// Initialises a new instance of <see cref="GlobalExceptionFilter"/>.
    /// </summary>
    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger,
        ITempDataDictionaryFactory tempDataFactory)
    {
        _logger = logger;
        _tempDataFactory = tempDataFactory;
    }

    /// <inheritdoc />
    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception,
            "Unhandled exception in {Controller}.{Action}",
            context.RouteData.Values["controller"],
            context.RouteData.Values["action"]);

        var friendlyMessage = GetUserFriendlyMessage(context.Exception);
        var isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                     || (context.HttpContext.Request.ContentType?.Contains("application/json") == true
                         && context.HttpContext.Request.Method != HttpMethods.Get);

        if (isAjax)
        {
            context.Result = new ObjectResult(new { error = friendlyMessage })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
        else
        {
            var tempData = _tempDataFactory.GetTempData(context.HttpContext);
            tempData["Error"] = friendlyMessage;

            context.Result = new RedirectToActionResult("Index", "Home", null);
        }

        context.ExceptionHandled = true;
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
