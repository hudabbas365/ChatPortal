using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class WorkspaceController : Controller
{
    private readonly AppDbContext _context;

    public WorkspaceController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private async Task<int?> GetActiveOrganizationIdAsync()
    {
        var orgId = HttpContext.Session.GetInt32("ActiveOrganizationId");
        if (orgId.HasValue)
            return orgId.Value;

        // Get user's first organization
        var userId = GetUserId();
        var firstOrg = await _context.Organizations
            .Where(o => o.OwnerId == userId || o.Members.Any(m => m.UserId == userId && m.IsActive))
            .Select(o => o.Id)
            .FirstOrDefaultAsync();

        if (firstOrg > 0)
        {
            HttpContext.Session.SetInt32("ActiveOrganizationId", firstOrg);
            return firstOrg;
        }

        return null;
    }

    // GET: Workspace/Index
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var organizationId = await GetActiveOrganizationIdAsync();

        if (!organizationId.HasValue)
        {
            TempData["Error"] = "Please create or select an organization first.";
            return RedirectToAction("Index", "Organization");
        }

        var workspaces = await _context.Workspaces
            .Include(w => w.Organization)
            .Include(w => w.Team)
            .Include(w => w.Agents)
            .Where(w => w.OrganizationId == organizationId.Value)
            .ToListAsync();

        return View(workspaces);
    }

    // POST: Workspace/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description, string context = "general", int? teamId = null)
    {
        try
        {
            var userId = GetUserId();
            var organizationId = await GetActiveOrganizationIdAsync();

            if (!organizationId.HasValue)
                return Json(new { success = false, error = "No active organization. Please create or select an organization first." });

            // Verify user has access to the organization
            var organization = await _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == organizationId.Value);

            if (organization == null)
                return Json(new { success = false, error = "Organization not found" });

            var isOwner = organization.OwnerId == userId;
            var isAdmin = organization.Members.Any(m => m.UserId == userId && m.Role == "Admin" && m.IsActive);
            var isMember = organization.Members.Any(m => m.UserId == userId && m.IsActive);

            if (!isOwner && !isAdmin && !isMember)
                return Json(new { success = false, error = "Access denied. You must be a member of the organization." });

            // Check if workspace name is unique within the organization
            var exists = await _context.Workspaces
                .AnyAsync(w => w.OrganizationId == organizationId.Value && w.Name == name);

            if (exists)
                return Json(new { success = false, error = $"A workspace named '{name}' already exists in this organization" });

            // Verify team ownership if teamId is provided
            if (teamId.HasValue)
            {
                var team = await _context.Teams
                    .Include(t => t.Members)
                    .FirstOrDefaultAsync(t => t.Id == teamId.Value && t.OrganizationId == organizationId.Value);

                if (team == null)
                    return Json(new { success = false, error = "Team not found in this organization" });
            }

            var workspace = new Workspace
            {
                Name = name,
                Description = description,
                OwnerId = userId,
                OrganizationId = organizationId.Value,
                TeamId = teamId,
                ChatAgentContext = context,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            return Json(new { success = true, workspaceId = workspace.Id, message = "Workspace created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: Workspace/GetUserWorkspaces
    [HttpGet]
    public async Task<IActionResult> GetUserWorkspaces()
    {
        var userId = GetUserId();
        var organizationId = await GetActiveOrganizationIdAsync();

        if (!organizationId.HasValue)
            return Json(new { success = false, error = "No active organization" });

        var workspaces = await _context.Workspaces
            .Include(w => w.Team)
            .Include(w => w.Agents)
            .Where(w => w.OrganizationId == organizationId.Value)
            .Select(w => new
            {
                w.Id,
                w.Name,
                w.Description,
                w.ChatAgentContext,
                w.IsActive,
                TeamName = w.Team != null ? w.Team.Name : "None",
                AgentCount = w.Agents.Count,
                IsOwner = w.OwnerId == userId
            })
            .ToListAsync();

        return Json(new { success = true, workspaces });
    }

    // POST: Workspace/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? description, string context, bool isActive)
    {
        // Edit Workspace feature is coming soon
        return Json(new { success = false, error = "Coming Soon - Edit Workspace feature is under development" });
    }

    // POST: Workspace/UpdateInternal (internal implementation, not exposed to users)
    private async Task<IActionResult> UpdateInternal(int id, string name, string? description, string context, bool isActive)
    {
        try
        {
            var userId = GetUserId();
            var organizationId = await GetActiveOrganizationIdAsync();

            if (!organizationId.HasValue)
                return Json(new { success = false, error = "No active organization" });

            var workspace = await _context.Workspaces
                .Include(w => w.Organization)
                .ThenInclude(o => o.Members)
                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId.Value);

            if (workspace == null)
                return Json(new { success = false, error = "Workspace not found" });

            // Check permissions
            var isOwner = workspace.OwnerId == userId;
            var isOrgOwner = workspace.Organization.OwnerId == userId;
            var isOrgAdmin = workspace.Organization.Members.Any(m => m.UserId == userId && m.Role == "Admin" && m.IsActive);

            if (!isOwner && !isOrgOwner && !isOrgAdmin)
                return Json(new { success = false, error = "Access denied" });

            // Check name uniqueness if changed
            if (workspace.Name != name)
            {
                var exists = await _context.Workspaces
                    .AnyAsync(w => w.OrganizationId == organizationId.Value && w.Name == name && w.Id != id);

                if (exists)
                    return Json(new { success = false, error = $"A workspace named '{name}' already exists in this organization" });
            }

            workspace.Name = name;
            workspace.Description = description;
            workspace.ChatAgentContext = context;
            workspace.IsActive = isActive;
            workspace.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Workspace updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Workspace/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            var organizationId = await GetActiveOrganizationIdAsync();

            if (!organizationId.HasValue)
                return Json(new { success = false, error = "No active organization" });

            var workspace = await _context.Workspaces
                .Include(w => w.Organization)
                .ThenInclude(o => o.Members)
                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId.Value);

            if (workspace == null)
                return Json(new { success = false, error = "Workspace not found" });

            // Check permissions
            var isOwner = workspace.OwnerId == userId;
            var isOrgOwner = workspace.Organization.OwnerId == userId;
            var isOrgAdmin = workspace.Organization.Members.Any(m => m.UserId == userId && m.Role == "Admin" && m.IsActive);

            if (!isOwner && !isOrgOwner && !isOrgAdmin)
                return Json(new { success = false, error = "Access denied" });

            _context.Workspaces.Remove(workspace);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Workspace deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Workspace/SetActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id)
    {
        try
        {
            var userId = GetUserId();
            var organizationId = await GetActiveOrganizationIdAsync();

            if (!organizationId.HasValue)
                return Json(new { success = false, error = "No active organization" });

            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == id && w.OrganizationId == organizationId.Value);

            if (workspace == null)
                return Json(new { success = false, error = "Workspace not found" });

            // Store in session
            HttpContext.Session.SetInt32("ActiveWorkspaceId", id);

            return Json(new { success = true, workspaceName = workspace.Name, context = workspace.ChatAgentContext });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}
