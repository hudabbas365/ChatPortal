using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatPortal.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Dashboard>> GetUserDashboardsAsync(int userId)
    {
        return await _db.Dashboards
            .Where(d => d.UserId == userId)
            .Include(d => d.PinnedCharts)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dashboard?> GetByIdAsync(int id, int userId)
    {
        return await _db.Dashboards
            .Include(d => d.PinnedCharts)
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
    }

    public async Task<Dashboard?> GetBySlugAsync(string slug)
    {
        return await _db.Dashboards
            .Include(d => d.PinnedCharts)
            .FirstOrDefaultAsync(d => d.PublicSlug == slug && d.IsPublic && !d.IsRevoked);
    }

    public async Task<Dashboard> CreateAsync(int userId, string title, string? description)
    {
        var dashboard = new Dashboard
        {
            UserId = userId,
            Title = title,
            Description = description,
            PublicSlug = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Dashboards.Add(dashboard);
        await _db.SaveChangesAsync();
        return dashboard;
    }

    public async Task<Dashboard> UpdateAsync(int id, int userId, string title, string? description, bool isPublic)
    {
        var dashboard = await _db.Dashboards.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException("Dashboard not found.");

        dashboard.Title = title;
        dashboard.Description = description;
        dashboard.IsPublic = isPublic;
        dashboard.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return dashboard;
    }

    public async Task DeleteAsync(int id, int userId)
    {
        var dashboard = await _db.Dashboards.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException("Dashboard not found.");
        _db.Dashboards.Remove(dashboard);
        await _db.SaveChangesAsync();
    }

    public async Task<Dashboard> ShareAsync(int id, int userId)
    {
        var dashboard = await _db.Dashboards.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException("Dashboard not found.");

        dashboard.IsPublic = true;
        dashboard.IsRevoked = false;
        dashboard.EmbedToken = Guid.NewGuid().ToString("N");
        dashboard.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return dashboard;
    }

    public async Task<PinnedChart> PinChartAsync(int userId, int queryHistoryId, int? dashboardId, string title, string chartDataJson, int position)
    {
        var pinned = new PinnedChart
        {
            UserId = userId,
            QueryHistoryId = queryHistoryId,
            DashboardId = dashboardId,
            Title = title,
            ChartDataJson = chartDataJson,
            Position = position,
            CreatedAt = DateTime.UtcNow
        };
        _db.PinnedCharts.Add(pinned);
        await _db.SaveChangesAsync();
        return pinned;
    }

    public async Task<List<PinnedChart>> GetPinnedChartsAsync(int dashboardId, int userId)
    {
        var dashboard = await _db.Dashboards.FirstOrDefaultAsync(d => d.Id == dashboardId && d.UserId == userId)
            ?? throw new KeyNotFoundException("Dashboard not found.");

        return await _db.PinnedCharts
            .Where(p => p.DashboardId == dashboardId)
            .OrderBy(p => p.Position)
            .ToListAsync();
    }

    public async Task UnpinChartAsync(int pinnedChartId, int userId)
    {
        var pinned = await _db.PinnedCharts.FirstOrDefaultAsync(p => p.Id == pinnedChartId && p.UserId == userId)
            ?? throw new KeyNotFoundException("Pinned chart not found.");
        _db.PinnedCharts.Remove(pinned);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Dashboard>> GetAllPublicDashboardsAsync()
    {
        return await _db.Dashboards
            .Where(d => d.IsPublic)
            .Include(d => d.User)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task RevokeShareAsync(int id)
    {
        var dashboard = await _db.Dashboards.FindAsync(id)
            ?? throw new KeyNotFoundException("Dashboard not found.");
        dashboard.IsRevoked = true;
        dashboard.IsPublic = false;
        dashboard.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
