using ChatPortal.Data;
using ChatPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace ChatPortal.Controllers;

[Authorize]
public class ChartController : Controller
{
    private readonly AppDbContext _context;
    private readonly IDashboardService _dashboardService;

    public ChartController(AppDbContext context, IDashboardService dashboardService)
    {
        _context = context;
        _dashboardService = dashboardService;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // GET: Chart/Index
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var dashboards = await _dashboardService.GetUserDashboardsAsync(userId);
        return View(dashboards);
    }

    // GET: Chart/Visuals
    public async Task<IActionResult> Visuals()
    {
        var userId = GetUserId();
        var pinnedCharts = await _context.PinnedCharts
            .Where(p => p.UserId == userId)
            .Include(p => p.QueryHistory)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(pinnedCharts);
    }

    // POST: Chart/Generate
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Generate([FromBody] GenerateChartRequest request)
    {
        try
        {
            if (request.Data == null || !request.Data.Any())
                return Json(new { success = false, error = "No data provided" });

            var chartType = request.ChartType?.ToLower() ?? "bar";
            var allowedTypes = new[] { "bar", "pie", "line", "doughnut" };
            if (!allowedTypes.Contains(chartType))
                chartType = "bar";

            // Build chart data from query result JSON
            var labels = new List<string>();
            var values = new List<double>();

            foreach (var row in request.Data.Take(20))
            {
                var rowDict = row as System.Collections.Generic.Dictionary<string, object>
                    ?? JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(
                        JsonSerializer.Serialize(row));

                if (rowDict == null) continue;

                var keys = rowDict.Keys.ToList();
                if (keys.Count >= 2)
                {
                    labels.Add(rowDict[keys[0]]?.ToString() ?? "");
                    if (double.TryParse(rowDict[keys[1]]?.ToString(), out var val))
                        values.Add(val);
                }
            }

            var pastelColors = new[]
            {
                "rgba(173, 216, 230, 0.7)",
                "rgba(216, 191, 216, 0.7)",
                "rgba(144, 238, 144, 0.7)",
                "rgba(255, 218, 185, 0.7)",
                "rgba(255, 255, 153, 0.7)",
                "rgba(176, 224, 230, 0.7)"
            };

            var chartData = new
            {
                chartType,
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = request.Label ?? "Value",
                        data = values,
                        backgroundColor = chartType is "pie" or "doughnut"
                            ? (object)pastelColors.Take(values.Count).ToArray()
                            : pastelColors[0],
                        borderColor = "rgba(100,149,237,0.9)",
                        borderWidth = 1
                    }
                }
            };

            return Json(new { success = true, chartData });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Chart/RenderHtml
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult RenderHtml([FromBody] RenderChartRequest request)
    {
        var allowedTypes = new[] { "bar", "pie", "line", "doughnut", "radar" };
        var chartType = request.ChartType?.ToLower() ?? "bar";
        if (!allowedTypes.Contains(chartType)) chartType = "bar";

        // Sanitize inputs
        var labels = request.Labels ?? new List<string>();
        var datasets = request.Datasets ?? new List<object>();
        var title = string.IsNullOrWhiteSpace(request.Title) ? "Chart" : request.Title;

        // Build fallback sample data when no real data provided
        string datasetsJs;
        string labelsJs;
        bool useFallback = !labels.Any() || !datasets.Any();

        if (useFallback)
        {
            labelsJs = "['Jan','Feb','Mar','Apr','May','Jun']";
            datasetsJs = @"[
  { label: 'Sample Bar', data: [42,67,35,89,54,71], backgroundColor: 'rgba(173,216,230,0.7)', borderColor: 'rgba(100,149,237,0.9)', borderWidth: 1, type: 'bar' },
  { label: 'Sample Line', data: [30,55,45,70,60,80], borderColor: 'rgba(144,238,144,0.9)', backgroundColor: 'rgba(144,238,144,0.2)', borderWidth: 2, type: 'line', fill: true },
  { label: 'Sample Radar', data: [65,59,90,81,56,72], borderColor: 'rgba(216,191,216,0.9)', backgroundColor: 'rgba(216,191,216,0.3)', borderWidth: 1 }
]";
            chartType = "bar";
        }
        else
        {
            labelsJs = System.Text.Json.JsonSerializer.Serialize(labels);
            datasetsJs = System.Text.Json.JsonSerializer.Serialize(datasets);
        }

        var html = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{System.Net.WebUtility.HtmlEncode(title)}</title>
<script src=""https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js""></script>
<style>
html,body{{margin:0;padding:0;width:100%;height:100%;background:#fff;}}
.chart-wrap{{width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:8px;box-sizing:border-box;}}
canvas{{max-width:100%;max-height:100%;}}
h3{{font-family:sans-serif;margin:0 0 8px;font-size:14px;color:#333;}}
</style>
</head>
<body>
<div class=""chart-wrap"">
  <h3>{System.Net.WebUtility.HtmlEncode(title)}</h3>
  <canvas id=""chartCanvas""></canvas>
</div>
<script>
(function(){{
  var ctx = document.getElementById('chartCanvas').getContext('2d');
  new Chart(ctx, {{
    type: '{chartType}',
    data: {{
      labels: {labelsJs},
      datasets: {datasetsJs}
    }},
    options: {{
      responsive: true,
      maintainAspectRatio: true,
      plugins: {{
        legend: {{ position: 'top' }},
        title: {{ display: false }}
      }}
    }}
  }});
}})();
</script>
</body>
</html>";
        return Content(html, "text/html");
    }

    // POST: Chart/Pin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pin(int queryHistoryId, int? dashboardId, string title, string chartDataJson, int position = 0)
    {
        try
        {
            var userId = GetUserId();
            var pinned = await _dashboardService.PinChartAsync(userId, queryHistoryId, dashboardId, title, chartDataJson, position);
            return Json(new { success = true, pinnedChartId = pinned.Id, message = "Chart pinned successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Chart/Unpin
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpin(int pinnedChartId)
    {
        try
        {
            var userId = GetUserId();
            await _dashboardService.UnpinChartAsync(pinnedChartId, userId);
            return Json(new { success = true, message = "Chart unpinned successfully" });
        }
        catch (KeyNotFoundException)
        {
            return Json(new { success = false, error = "Pinned chart not found" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Chart/Export
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Export([FromBody] ExportChartRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Html))
                return Json(new { success = false, error = "No HTML content provided" });

            // Return PDF-ready HTML with chart.js CDN included
            var html = "<!DOCTYPE html>\n" +
                "<html>\n" +
                "<head>\n" +
                "<meta charset=\"utf-8\"/>\n" +
                $"<title>{request.Title ?? "Chart Export"}</title>\n" +
                "<script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>\n" +
                "<style>body{font-family:sans-serif;padding:20px;}</style>\n" +
                "</head>\n" +
                "<body>\n" +
                request.Html + "\n" +
                "</body>\n" +
                "</html>";
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Chart/Share
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Share(int dashboardId)
    {
        try
        {
            var userId = GetUserId();
            var dashboard = await _dashboardService.ShareAsync(dashboardId, userId);
            var shareUrl = Url.Action("View", "Embed", new { slug = dashboard.PublicSlug }, Request.Scheme);
            return Json(new { success = true, shareUrl, slug = dashboard.PublicSlug });
        }
        catch (KeyNotFoundException)
        {
            return Json(new { success = false, error = "Dashboard not found" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Chart/DeleteShareLink
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteShareLink(int dashboardId)
    {
        try
        {
            var userId = GetUserId();
            // Verify ownership before revoking
            var dashboard = await _dashboardService.GetByIdAsync(dashboardId, userId);
            if (dashboard == null)
                return Json(new { success = false, error = "Dashboard not found or access denied" });

            await _dashboardService.RevokeShareAsync(dashboardId);
            return Json(new { success = true, message = "Share link revoked successfully" });
        }
        catch (KeyNotFoundException)
        {
            return Json(new { success = false, error = "Dashboard not found" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}

public class RenderChartRequest
{
    public string? ChartType { get; set; }
    public List<string>? Labels { get; set; }
    public List<object>? Datasets { get; set; }
    public string? Title { get; set; }
}

public class GenerateChartRequest
{
    public List<object>? Data { get; set; }
    public string? ChartType { get; set; }
    public string? Label { get; set; }
}

public class ExportChartRequest
{
    public string? Html { get; set; }
    public string? Title { get; set; }
}
