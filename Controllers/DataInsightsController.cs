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

    public DataInsightsController(IDataConnectionService dataConnection, IDataChatService dataChatService, ICreditService creditService)
    {
        _dataConnection = dataConnection;
        _dataChatService = dataChatService;
        _creditService = creditService;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> Index(int dataSourceId)
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Query(int dataSourceId, [FromBody] string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest(new { error = "Question is required." });

        var userId = GetUserId();
        var result = await _dataChatService.QueryDataSourceAsync(userId, dataSourceId, question);
        return Json(result);
    }
}
