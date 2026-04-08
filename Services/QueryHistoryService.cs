using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatPortal.Services;

public class QueryHistoryService : IQueryHistoryService
{
    private readonly AppDbContext _db;

    public QueryHistoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<QueryHistory> SaveAsync(Guid userId, Guid dataSourceId, string query, string? resultJson, string? chartDataJson, string? narrative)
    {
        var entry = new QueryHistory
        {
            UserId = userId,
            DataSourceId = dataSourceId,
            Query = query,
            ResultJson = resultJson,
            ChartDataJson = chartDataJson,
            Narrative = narrative,
            CreatedAt = DateTime.UtcNow
        };
        _db.QueryHistories.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<List<QueryHistory>> GetHistoryAsync(Guid userId, Guid? dataSourceId = null, int page = 1, int pageSize = 20)
    {
        var query = _db.QueryHistories
            .Where(q => q.UserId == userId);

        if (dataSourceId.HasValue)
            query = query.Where(q => q.DataSourceId == dataSourceId.Value);

        return await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(q => q.DataSource)
            .ToListAsync();
    }

    public async Task<QueryHistory?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _db.QueryHistories
            .Include(q => q.DataSource)
            .FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entry = await _db.QueryHistories.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId)
            ?? throw new KeyNotFoundException("History entry not found.");
        _db.QueryHistories.Remove(entry);
        await _db.SaveChangesAsync();
    }
}
