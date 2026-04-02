using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

public class ErrorController : Controller
{
    [Route("Error/{statusCode}")]
    public IActionResult Index(int statusCode)
    {
        ViewBag.StatusCode = statusCode;
        ViewBag.Message = statusCode switch
        {
            404 => "Page not found",
            403 => "Access forbidden",
            500 => "Internal server error",
            _ => "An error occurred"
        };
        return View();
    }

    [Route("Error/ServerError")]
    public IActionResult ServerError()
    {
        ViewBag.StatusCode = 500;
        ViewBag.Message = "An internal server error occurred.";
        return View("Index");
    }
}
