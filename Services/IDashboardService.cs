using ChatPortal.Models.Entities;

namespace ChatPortal.Services;

public interface IDashboardService
{
    Task<List<Dashboard>> GetUserDashboardsAsync(Guid userId);
    Task<Dashboard?> GetByIdAsync(Guid id, Guid userId);
    Task<Dashboard?> GetBySlugAsync(string slug);
    Task<Dashboard> CreateAsync(Guid userId, string title, string? description);
    Task<Dashboard> UpdateAsync(Guid id, Guid userId, string title, string? description, bool isPublic);
    Task DeleteAsync(Guid id, Guid userId);
    Task<Dashboard> ShareAsync(Guid id, Guid userId);
    Task<PinnedChart> PinChartAsync(Guid userId, Guid queryHistoryId, Guid? dashboardId, string title, string chartDataJson, int position);
    Task<List<PinnedChart>> GetPinnedChartsAsync(Guid dashboardId, Guid userId);
    Task UnpinChartAsync(Guid pinnedChartId, Guid userId);
    Task<List<Dashboard>> GetAllPublicDashboardsAsync();
    Task RevokeShareAsync(Guid id);
}
