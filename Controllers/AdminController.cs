using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

public class AdminController : Controller
{
    public IActionResult Index()
    {
        ViewBag.TotalUsers = 1247;
        ViewBag.ActiveSubscriptions = 342;
        ViewBag.TotalRevenue = "$18,432";
        ViewBag.ApiCalls = "48,291";
        return View();
    }
}
