using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class DataInsightsController : Controller
{
    private readonly IDataConnectionService _dataConnection;
    private readonly IDataChatService _dataChatService;
    private readonly ICreditService _creditService;
    private readonly ILogger<DataInsightsController> _logger;

    public DataInsightsController(IDataConnectionService dataConnection, IDataChatService dataChatService,
        ICreditService creditService, ILogger<DataInsightsController> logger)
    {
        _dataConnection = dataConnection;
        _dataChatService = dataChatService;
        _creditService = creditService;
        _logger = logger;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> Index(int dataSourceId)
    {
        try
        {
            var userId = GetUserId();
            var ds = await _dataConnection.GetDataSourceAsync(dataSourceId, userId);
            if (ds == null)
            {
                TempData["Error"] = "Data source not found.";
                return RedirectToAction("Index", "DataConnection");
            }

            var vm = new DataInsightsViewModel
            {
                DataSource = ds,
                CreditBalance = await _creditService.GetBalanceAsync(userId)
            };
            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data insights for source {DataSourceId}", dataSourceId);
            TempData["Error"] = "Unable to load data source insights. Please try again.";
            return RedirectToAction("Index", "DataConnection");
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Query(int dataSourceId, [FromBody] string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest(new { error = "Question is required." });

        try
        {
            var userId = GetUserId();
            var result = await _dataChatService.QueryDataSourceAsync(userId, dataSourceId, question);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying data source {DataSourceId}", dataSourceId);
            return StatusCode(500, new { success = false, error = "An error occurred while processing your query. Please try again." });
        }
    }
}
