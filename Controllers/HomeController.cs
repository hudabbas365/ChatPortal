using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
