using ChatPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers.Api;

[ApiController]
[Route("api/connectors")]
[Authorize]
public class ConnectorApiController : ControllerBase
{
    private readonly IDataConnectionService _dataConnection;
    private readonly ILogger<ConnectorApiController> _logger;

    public ConnectorApiController(IDataConnectionService dataConnection, ILogger<ConnectorApiController> logger)
    {
        _dataConnection = dataConnection;
        _logger = logger;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>Returns schema/tables for a data source.</summary>
    [HttpGet("{dataSourceId:int}/schema")]
    public async Task<IActionResult> GetSchema(int dataSourceId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var schema = await _dataConnection.GetSchemaAsync(dataSourceId, userId.Value);
            return Ok(new { success = true, schema });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Data source not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching schema for data source {DataSourceId}", dataSourceId);
            return StatusCode(500, new { success = false, error = "An error occurred fetching schema." });
        }
    }

    /// <summary>Returns data from a specific table in a data source.</summary>
    [HttpGet("{dataSourceId:int}/data")]
    public async Task<IActionResult> GetData(int dataSourceId, [FromQuery] string table, [FromQuery] int limit = 100)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(table))
            return BadRequest(new { success = false, error = "Table parameter is required." });

        limit = Math.Clamp(limit, 1, 500);

        try
        {
            var ds = await _dataConnection.GetDataSourceAsync(dataSourceId, userId.Value);
            if (ds == null) return NotFound(new { success = false, error = "Data source not found." });

            string query = ds.SourceType switch
            {
                "SqlServer" => $"SELECT TOP {limit} * FROM {table}",
                "PostgreSQL" => $"SELECT * FROM {table} LIMIT {limit}",
                "MySQL" => $"SELECT * FROM {table} LIMIT {limit}",
                "Oracle" => $"SELECT * FROM {table} FETCH FIRST {limit} ROWS ONLY",
                _ => table
            };

            var data = await _dataConnection.QueryDataSourceAsync(dataSourceId, userId.Value, query);
            return Ok(new { success = true, table, rowCount = data.Count, data });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Data source not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data from {Table} in data source {DataSourceId}", table, dataSourceId);
            return StatusCode(500, new { success = false, error = "An error occurred fetching data." });
        }
    }

    /// <summary>Executes a query against a data source.</summary>
    [HttpPost("{dataSourceId:int}/query")]
    public async Task<IActionResult> ExecuteQuery(int dataSourceId, [FromBody] QueryRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request?.Query))
            return BadRequest(new { success = false, error = "Query is required." });

        try
        {
            var data = await _dataConnection.ExecuteQueryAsync(dataSourceId, userId.Value, request.Query);
            return Ok(new { success = true, rowCount = data.Count, data });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Data source not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query on data source {DataSourceId}", dataSourceId);
            return StatusCode(500, new { success = false, error = "An error occurred executing the query." });
        }
    }

    /// <summary>Returns connection status for a data source.</summary>
    [HttpGet("{dataSourceId:int}/status")]
    public async Task<IActionResult> GetStatus(int dataSourceId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var ds = await _dataConnection.GetDataSourceAsync(dataSourceId, userId.Value);
            if (ds == null) return NotFound(new { success = false, error = "Data source not found." });

            var isConnected = ds.SourceType is "Excel" or "CSV"
                ? !string.IsNullOrEmpty(ds.FilePath)
                : await _dataConnection.ValidateConnectionAsync(ds.SourceType, ds.ConnectionDetails ?? string.Empty);

            return Ok(new
            {
                success = true,
                dataSourceId,
                name = ds.Name,
                sourceType = ds.SourceType,
                status = ds.Status,
                isConnected
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking status for data source {DataSourceId}", dataSourceId);
            return StatusCode(500, new { success = false, error = "An error occurred checking connection status." });
        }
    }
}

public record QueryRequest(string Query);
