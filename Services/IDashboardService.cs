using ChatPortal.Models.Entities;

namespace ChatPortal.Services;

public interface IDashboardService
{
    Task<List<Dashboard>> GetUserDashboardsAsync(int userId);
    Task<Dashboard?> GetByIdAsync(int id, int userId);
    Task<Dashboard?> GetBySlugAsync(string slug);
    Task<Dashboard> CreateAsync(int userId, string title, string? description);
    Task<Dashboard> UpdateAsync(int id, int userId, string title, string? description, bool isPublic);
    Task DeleteAsync(int id, int userId);
    Task<Dashboard> ShareAsync(int id, int userId);
    Task<PinnedChart> PinChartAsync(int userId, int queryHistoryId, int? dashboardId, string title, string chartDataJson, int position);
    Task<List<PinnedChart>> GetPinnedChartsAsync(int dashboardId, int userId);
    Task UnpinChartAsync(int pinnedChartId, int userId);
    Task<List<Dashboard>> GetAllPublicDashboardsAsync();
    Task RevokeShareAsync(int id);
}
