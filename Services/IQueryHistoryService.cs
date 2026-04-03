using ChatPortal.Models.Entities;

namespace ChatPortal.Services;

public interface IQueryHistoryService
{
    Task<QueryHistory> SaveAsync(int userId, int dataSourceId, string query, string? resultJson, string? chartDataJson, string? narrative);
    Task<List<QueryHistory>> GetHistoryAsync(int userId, int? dataSourceId = null, int page = 1, int pageSize = 20);
    Task<QueryHistory?> GetByIdAsync(int id, int userId);
    Task DeleteAsync(int id, int userId);
}
