using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class AgentController : Controller
{
    private readonly AppDbContext _context;

    public AgentController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private async Task<int?> GetActiveOrganizationIdAsync()
    {
        var orgId = HttpContext.Session.GetInt32("ActiveOrganizationId");
        if (orgId.HasValue) return orgId.Value;

        var userId = GetUserId();
        var org = await _context.Organizations
            .Where(o => o.OwnerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (org != null)
        {
            HttpContext.Session.SetInt32("ActiveOrganizationId", org.Id);
            return org.Id;
        }

        return null;
    }

    // POST: Agent/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int workspaceId, string name, string? description, 
        string agentType = "general", string? systemPrompt = null, string? modelName = "GPT-3.5 Turbo",
        decimal temperature = 0.7m, int maxTokens = 2000)
    {
        try
        {
            var userId = GetUserId();
            var orgId = await GetActiveOrganizationIdAsync();

            if (!orgId.HasValue)
                return Json(new { success = false, error = "No active organization. Please create or select an organization first." });

            // Verify workspace belongs to organization and user has access
            var workspace = await _context.Workspaces
                .Include(w => w.Organization)
                .ThenInclude(o => o.Members)
                .FirstOrDefaultAsync(w => w.Id == workspaceId && w.OrganizationId == orgId.Value);

            if (workspace == null)
                return Json(new { success = false, error = "Workspace not found or access denied" });

            // Check if user has permission (Owner, Admin, or Member)
            var isOwner = workspace.Organization.OwnerId == userId;
            var isMember = workspace.Organization.Members.Any(m => m.UserId == userId && 
                (m.Role == "Owner" || m.Role == "Admin" || m.Role == "Member"));

            if (!isOwner && !isMember)
                return Json(new { success = false, error = "You don't have permission to create agents in this workspace" });

            var agent = new Agent
            {
                Name = name,
                Description = description,
                WorkspaceId = workspaceId,
                AgentType = agentType,
                SystemPrompt = systemPrompt,
                ModelName = modelName,
                Temperature = temperature,
                MaxTokens = maxTokens,
                IsActive = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                agentId = agent.Id, 
                message = "AI Agent created successfully" 
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: Agent/GetWorkspaceAgents
    [HttpGet]
    public async Task<IActionResult> GetWorkspaceAgents(int workspaceId)
    {
        try
        {
            var userId = GetUserId();
            var orgId = await GetActiveOrganizationIdAsync();

            if (!orgId.HasValue)
                return Json(new { success = false, error = "No active organization" });

            // Verify access to workspace
            var workspace = await _context.Workspaces
                .Include(w => w.Organization)
                .ThenInclude(o => o.Members)
                .FirstOrDefaultAsync(w => w.Id == workspaceId && w.OrganizationId == orgId.Value);

            if (workspace == null)
                return Json(new { success = false, error = "Workspace not found or access denied" });

            var agents = await _context.Agents
                .Where(a => a.WorkspaceId == workspaceId)
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Description,
                    a.AgentType,
                    a.ModelName,
                    a.Temperature,
                    a.MaxTokens,
                    a.IsActive,
                    ChatSessionCount = a.ChatSessions.Count,
                    IsCreator = a.CreatedBy == userId,
                    CreatedAt = a.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Json(new { success = true, agents });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Agent/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string? name, string? description,
        string? agentType, string? systemPrompt, string? modelName,
        decimal? temperature, int? maxTokens, bool? isActive)
    {
        try
        {
            var userId = GetUserId();
            var agent = await _context.Agents
                .Include(a => a.Workspace)
                .ThenInclude(w => w.Organization)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agent == null)
                return Json(new { success = false, error = "Agent not found" });

            // Check permissions
            var isOwner = agent.Workspace.Organization.OwnerId == userId;
            var isCreator = agent.CreatedBy == userId;

            if (!isOwner && !isCreator)
                return Json(new { success = false, error = "You don't have permission to update this agent" });

            if (!string.IsNullOrEmpty(name)) agent.Name = name;
            if (description != null) agent.Description = description;
            if (!string.IsNullOrEmpty(agentType)) agent.AgentType = agentType;
            if (systemPrompt != null) agent.SystemPrompt = systemPrompt;
            if (!string.IsNullOrEmpty(modelName)) agent.ModelName = modelName;
            if (temperature.HasValue) agent.Temperature = temperature.Value;
            if (maxTokens.HasValue) agent.MaxTokens = maxTokens.Value;
            if (isActive.HasValue) agent.IsActive = isActive.Value;
            
            agent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Agent updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Agent/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            var agent = await _context.Agents
                .Include(a => a.Workspace)
                .ThenInclude(w => w.Organization)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agent == null)
                return Json(new { success = false, error = "Agent not found" });

            // Check permissions - only organization owner or agent creator can delete
            var isOwner = agent.Workspace.Organization.OwnerId == userId;
            var isCreator = agent.CreatedBy == userId;

            if (!isOwner && !isCreator)
                return Json(new { success = false, error = "You don't have permission to delete this agent" });

            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Agent deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Agent/ToggleActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        try
        {
            var userId = GetUserId();
            var agent = await _context.Agents
                .Include(a => a.Workspace)
                .ThenInclude(w => w.Organization)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agent == null)
                return Json(new { success = false, error = "Agent not found" });

            // Check permissions
            var isOwner = agent.Workspace.Organization.OwnerId == userId;
            var isCreator = agent.CreatedBy == userId;

            if (!isOwner && !isCreator)
                return Json(new { success = false, error = "You don't have permission to modify this agent" });

            agent.IsActive = !agent.IsActive;
            agent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                isActive = agent.IsActive,
                message = $"Agent {(agent.IsActive ? "activated" : "deactivated")} successfully" 
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}
