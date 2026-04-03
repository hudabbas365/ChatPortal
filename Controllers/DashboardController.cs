using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var dashboards = await _dashboardService.GetUserDashboardsAsync(userId.Value);
        return View(dashboards);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, string? description)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("title", "Title is required.");
            return View();
        }

        var dashboard = await _dashboardService.CreateAsync(userId.Value, title, description);
        TempData["Success"] = "Dashboard created successfully.";
        return RedirectToAction(nameof(View), new { id = dashboard.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var dashboard = await _dashboardService.GetByIdAsync(id, userId.Value);
        if (dashboard == null) return NotFound();

        return View(dashboard);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string title, string? description, bool isPublic)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        try
        {
            await _dashboardService.UpdateAsync(id, userId.Value, title, description, isPublic);
            TempData["Success"] = "Dashboard updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        try
        {
            await _dashboardService.DeleteAsync(id, userId.Value);
            TempData["Success"] = "Dashboard deleted.";
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Dashboard not found.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> View(int id)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var dashboard = await _dashboardService.GetByIdAsync(id, userId.Value);
        if (dashboard == null) return NotFound();

        return View(dashboard);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Share(int id)
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        try
        {
            var dashboard = await _dashboardService.ShareAsync(id, userId.Value);
            TempData["Success"] = $"Dashboard shared. Public URL: /embed/dashboard/{dashboard.PublicSlug}";
            return RedirectToAction(nameof(View), new { id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PinChart(int queryHistoryId, int? dashboardId, string title, string chartDataJson, int position = 0)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await _dashboardService.PinChartAsync(userId.Value, queryHistoryId, dashboardId, title, chartDataJson, position);
        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnpinChart(int pinnedChartId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _dashboardService.UnpinChartAsync(pinnedChartId, userId.Value);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false });
        }
    }
}
