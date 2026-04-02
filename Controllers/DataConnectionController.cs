using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class DataConnectionController : Controller
{
    private readonly IDataConnectionService _dataConnection;
    private readonly ICreditService _creditService;

    public DataConnectionController(IDataConnectionService dataConnection, ICreditService creditService)
    {
        _dataConnection = dataConnection;
        _creditService = creditService;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var vm = new DataConnectionViewModel
        {
            DataSources = await _dataConnection.GetUserDataSourcesAsync(userId),
            CreditBalance = await _creditService.GetBalanceAsync(userId)
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadFile(CreateFileDataSourceViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var userId = GetUserId();
            await _dataConnection.CreateFileDataSourceAsync(userId, model.Name, model.SourceType, model.File);
            TempData["Success"] = $"Data source '{model.Name}' created successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = "An error occurred while uploading the file.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectDatabase(CreateDbDataSourceViewModel model)
    {
        try
        {
            var userId = GetUserId();
            await _dataConnection.CreateDatabaseDataSourceAsync(userId, model.Name, model.SourceType, model.ConnectionString);
            TempData["Success"] = $"Database '{model.Name}' connected successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Connection failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetTables(string sourceType, string connectionString)
    {
        try
        {
            var tables = await _dataConnection.GetAvailableTablesAsync(sourceType, connectionString);
            return Json(new { success = true, tables });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTables(int id, [FromBody] List<string> selectedTables)
    {
        try
        {
            var userId = GetUserId();
            await _dataConnection.UpdateSelectedTablesAsync(id, userId, selectedTables);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            await _dataConnection.DeleteDataSourceAsync(id, userId);
            TempData["Success"] = "Data source deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Delete failed: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }
}
