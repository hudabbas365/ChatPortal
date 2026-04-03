using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChatPortal.Data;
using ChatPortal.Models.Entities;
using ChatPortal.Services.DataSourceConnectors;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatPortal.Controllers
{
    [Authorize]
    public class DataSourceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public DataSourceController(AppDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        private async Task<int?> GetActiveOrganizationIdAsync()
        {
            var orgIdString = HttpContext.Session.GetString("ActiveOrganizationId");
            if (string.IsNullOrEmpty(orgIdString)) return null;
            return int.Parse(orgIdString);
        }

        [HttpGet]
        public IActionResult GetProviders()
        {
            try
            {
                var providersByCategory = DataSourceProviderRegistry.GetProvidersByCategory();
                
                return Json(new
                {
                    success = true,
                    categories = providersByCategory.Select(kvp => new
                    {
                        category = kvp.Key,
                        providers = kvp.Value.Select(p => new
                        {
                            id = p.Id,
                            name = p.Name,
                            description = p.Description,
                            icon = p.Icon,
                            requiresOAuth = p.RequiresOAuth,
                            documentationUrl = p.DocumentationUrl
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestConnection([FromForm] int connectionId)
        {
            try
            {
                var userId = GetUserId();
                var connection = await _context.DataSourceConnections
                    .Where(c => c.Id == connectionId && c.UserId == userId && c.IsActive)
                    .FirstOrDefaultAsync();

                if (connection == null)
                {
                    return Json(new { success = false, error = "Connection not found" });
                }

                var provider = DataSourceProviderRegistry.GetAllProviders()
                    .FirstOrDefault(p => p.Id == connection.Provider.ToLower());

                if (provider == null)
                {
                    return Json(new { success = false, error = "Provider not supported" });
                }

                var connector = (IDataSourceConnector)ActivatorUtilities.CreateInstance(_serviceProvider, provider.ConnectorType);
                var result = await connector.TestConnectionAsync(connection);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    error = result.ErrorDetails,
                    metadata = result.Metadata
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect([FromForm] string provider, [FromForm] string name, 
            [FromForm] string? connectionString, [FromForm] string? apiEndpoint, 
            [FromForm] string? apiKey, [FromForm] string? username, [FromForm] string? password,
            [FromForm] string? additionalConfig)
        {
            try
            {
                var userId = GetUserId();
                var orgId = await GetActiveOrganizationIdAsync();

                var providerInfo = DataSourceProviderRegistry.GetAllProviders()
                    .FirstOrDefault(p => p.Id == provider.ToLower());

                if (providerInfo == null)
                {
                    return Json(new { success = false, error = "Provider not supported" });
                }

                var connection = new DataSourceConnection
                {
                    UserId = userId,
                    OrganizationId = orgId,
                    Name = name,
                    SourceType = providerInfo.Category,
                    Provider = provider,
                    ConnectionString = connectionString,
                    ApiEndpoint = apiEndpoint,
                    ApiKey = apiKey,
                    Username = username,
                    PasswordHash = password, // TODO: Hash the password
                    AdditionalConfig = additionalConfig,
                    IsActive = true,
                    ConnectedAt = DateTime.UtcNow
                };

                _context.DataSourceConnections.Add(connection);
                await _context.SaveChangesAsync();

                // Test the connection
                var connector = (IDataSourceConnector)ActivatorUtilities.CreateInstance(_serviceProvider, providerInfo.ConnectorType);
                var testResult = await connector.TestConnectionAsync(connection);

                if (testResult.Success)
                {
                    connection.LastSyncStatus = "Connected";
                    connection.LastSyncAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = testResult.Message,
                    connectionId = connection.Id,
                    connectionStatus = connection.LastSyncStatus
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disconnect([FromForm] int connectionId)
        {
            try
            {
                var userId = GetUserId();
                var connection = await _context.DataSourceConnections
                    .Where(c => c.Id == connectionId && c.UserId == userId)
                    .FirstOrDefaultAsync();

                if (connection == null)
                {
                    return Json(new { success = false, error = "Connection not found" });
                }

                var provider = DataSourceProviderRegistry.GetAllProviders()
                    .FirstOrDefault(p => p.Id == connection.Provider.ToLower());

                if (provider != null)
                {
                    var connector = (IDataSourceConnector)ActivatorUtilities.CreateInstance(_serviceProvider, provider.ConnectorType);
                    await connector.DisconnectAsync(connectionId);
                }

                return Json(new { success = true, message = "Connection disconnected successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetConnections()
        {
            try
            {
                var userId = GetUserId();
                var connections = await _context.DataSourceConnections
                    .Where(c => c.UserId == userId && c.IsActive)
                    .OrderByDescending(c => c.ConnectedAt)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        provider = c.Provider,
                        sourceType = c.SourceType,
                        connectedAt = c.ConnectedAt,
                        lastSyncAt = c.LastSyncAt,
                        lastSyncStatus = c.LastSyncStatus,
                        description = c.Description
                    })
                    .ToListAsync();

                return Json(new { success = true, connections });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetConnectionStatus(int connectionId)
        {
            try
            {
                var userId = GetUserId();
                var connection = await _context.DataSourceConnections
                    .Where(c => c.Id == connectionId && c.UserId == userId)
                    .FirstOrDefaultAsync();

                if (connection == null)
                {
                    return Json(new { success = false, error = "Connection not found" });
                }

                var provider = DataSourceProviderRegistry.GetAllProviders()
                    .FirstOrDefault(p => p.Id == connection.Provider.ToLower());

                if (provider == null)
                {
                    return Json(new { success = false, error = "Provider not supported" });
                }

                var connector = (IDataSourceConnector)ActivatorUtilities.CreateInstance(_serviceProvider, provider.ConnectorType);
                var health = await connector.GetHealthAsync(connectionId);

                return Json(new
                {
                    success = true,
                    isHealthy = health.IsHealthy,
                    status = health.Status,
                    lastChecked = health.LastChecked,
                    message = health.Message,
                    details = health.Details
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sync([FromForm] int connectionId)
        {
            try
            {
                var userId = GetUserId();
                var connection = await _context.DataSourceConnections
                    .Where(c => c.Id == connectionId && c.UserId == userId && c.IsActive)
                    .FirstOrDefaultAsync();

                if (connection == null)
                {
                    return Json(new { success = false, error = "Connection not found" });
                }

                var provider = DataSourceProviderRegistry.GetAllProviders()
                    .FirstOrDefault(p => p.Id == connection.Provider.ToLower());

                if (provider == null)
                {
                    return Json(new { success = false, error = "Provider not supported" });
                }

                var connector = (IDataSourceConnector)ActivatorUtilities.CreateInstance(_serviceProvider, provider.ConnectorType);
                var syncResult = await connector.SyncDataAsync(connectionId);

                return Json(new
                {
                    success = syncResult.Success,
                    recordsProcessed = syncResult.RecordsProcessed,
                    syncTime = syncResult.SyncTime,
                    message = syncResult.Message,
                    error = syncResult.ErrorDetails
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetProviderSchema(string provider)
        {
            try
            {
                var providerInfo = DataSourceProviderRegistry.GetAllProviders()
                    .FirstOrDefault(p => p.Id == provider.ToLower());

                if (providerInfo == null)
                {
                    return Json(new { success = false, error = "Provider not supported" });
                }

                var connector = (IDataSourceConnector)ActivatorUtilities.CreateInstance(_serviceProvider, providerInfo.ConnectorType);
                var schema = connector.GetConfigurationSchema();

                return Json(new
                {
                    success = true,
                    schema,
                    provider = new
                    {
                        id = providerInfo.Id,
                        name = providerInfo.Name,
                        description = providerInfo.Description,
                        requiresOAuth = providerInfo.RequiresOAuth
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
