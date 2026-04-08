using ChatPortal.Models.Entities;

namespace ChatPortal.Services;

public interface IQueryHistoryService
{
    Task<QueryHistory> SaveAsync(Guid userId, Guid dataSourceId, string query, string? resultJson, string? chartDataJson, string? narrative);
    Task<List<QueryHistory>> GetHistoryAsync(Guid userId, Guid? dataSourceId = null, int page = 1, int pageSize = 20);
    Task<QueryHistory?> GetByIdAsync(Guid id, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
