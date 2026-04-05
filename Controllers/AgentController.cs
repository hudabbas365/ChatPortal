using ChatPortal.Data;
using ChatPortal.Models.Entities;
using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class AgentController : Controller
{
    private readonly AppDbContext _context;
    private readonly IAIChatService _aiChatService;

    public AgentController(AppDbContext context, IAIChatService aiChatService)
    {
        _context = context;
        _aiChatService = aiChatService;
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

            var agentData = await _context.Agents
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

            var agents = agentData.Select(a => new
            {
                a.Id,
                a.Name,
                a.Description,
                a.AgentType,
                a.ModelName,
                a.Temperature,
                a.MaxTokens,
                a.IsActive,
                a.ChatSessionCount,
                a.IsCreator,
                a.CreatedAt,
                chatUrl = Url.Action("Chat", "Agent", new { id = a.Id })
            });

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

    // POST: Agent/BindDataSource
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BindDataSource(int agentId, int dataSourceId)
    {
        try
        {
            var userId = GetUserId();
            var agent = await _context.Agents
                .Include(a => a.Workspace)
                .ThenInclude(w => w.Organization)
                .FirstOrDefaultAsync(a => a.Id == agentId);

            if (agent == null)
                return Json(new { success = false, error = "Agent not found" });

            var isOwner = agent.Workspace.Organization.OwnerId == userId;
            var isCreator = agent.CreatedBy == userId;

            if (!isOwner && !isCreator)
                return Json(new { success = false, error = "You don't have permission to modify this agent" });

            var dataSource = await _context.UserDataSources
                .FirstOrDefaultAsync(ds => ds.Id == dataSourceId && ds.UserId == userId);

            if (dataSource == null)
                return Json(new { success = false, error = "Data source not found or access denied" });

            // Auto-generate system prompt from the spec template
            var systemPrompt = $"You are an AI Agent for {agent.Name}.\n" +
                               $"Generate queries for {dataSource.Name} using the following tables/views:\n" +
                               $"{dataSource.SchemaSnapshot ?? "No schema available"}.\n" +
                               "Maintain relationships between tables if they exist.";

            agent.DataSourceId = dataSourceId;
            agent.SystemPrompt = systemPrompt;
            agent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Data source bound successfully",
                systemMessage = systemPrompt
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Agent/UnbindDataSource
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnbindDataSource(int agentId)
    {
        try
        {
            var userId = GetUserId();
            var agent = await _context.Agents
                .Include(a => a.Workspace)
                .ThenInclude(w => w.Organization)
                .FirstOrDefaultAsync(a => a.Id == agentId);

            if (agent == null)
                return Json(new { success = false, error = "Agent not found" });

            var isOwner = agent.Workspace.Organization.OwnerId == userId;
            var isCreator = agent.CreatedBy == userId;

            if (!isOwner && !isCreator)
                return Json(new { success = false, error = "You don't have permission to modify this agent" });

            agent.DataSourceId = null;
            agent.SystemPrompt = null;
            agent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Data source unbound successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: Agent/Chat/{id}
    [HttpGet]
    public async Task<IActionResult> Chat(int id)
    {
        try
        {
            var userId = GetUserId();
            var agent = await _context.Agents
                .Include(a => a.Workspace)
                .ThenInclude(w => w.Organization)
                .ThenInclude(o => o.Members)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agent == null)
                return NotFound("Agent not found");

            var isOwner = agent.Workspace.Organization.OwnerId == userId;
            var isMember = agent.Workspace.Organization.Members.Any(m => m.UserId == userId);
            var isCreator = agent.CreatedBy == userId;

            if (!isOwner && !isMember && !isCreator)
                return Forbid();

            // Load or create a chat session for this agent+user
            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .Where(s => s.AgentId == id && s.UserId == userId && !s.IsArchived)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();

            var history = new List<ChatMessageDto>();
            int? sessionId = null;

            if (session != null)
            {
                sessionId = session.Id;
                history = session.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatMessageDto(m.Role, m.Content, m.CreatedAt))
                    .ToList();
            }

            var vm = new AgentChatViewModel
            {
                AgentId = agent.Id,
                AgentName = agent.Name,
                AgentDescription = agent.Description,
                AgentType = agent.AgentType,
                ModelName = agent.ModelName,
                SessionId = sessionId,
                History = history
            };

            ViewData["Title"] = $"Chat with {agent.Name}";
            return View(vm);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // POST: Agent/Chat
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] AgentChatRequest body)
    {
        try
        {
            var userId = GetUserId();

            var agent = await _context.Agents
                .Include(a => a.Workspace)
                .ThenInclude(w => w.Organization)
                .ThenInclude(o => o.Members)
                .FirstOrDefaultAsync(a => a.Id == body.AgentId);

            if (agent == null)
                return Json(new { success = false, error = "Agent not found" });

            var isOwner = agent.Workspace.Organization.OwnerId == userId;
            var isMember = agent.Workspace.Organization.Members.Any(m => m.UserId == userId);
            var isCreator = agent.CreatedBy == userId;

            if (!isOwner && !isMember && !isCreator)
                return Json(new { success = false, error = "Access denied" });

            // Load or create session
            ChatSession? session = null;
            if (body.SessionId.HasValue)
            {
                session = await _context.ChatSessions
                    .Include(s => s.Messages)
                    .FirstOrDefaultAsync(s => s.Id == body.SessionId.Value && s.UserId == userId);
            }

            if (session == null)
            {
                session = new ChatSession
                {
                    UserId = userId,
                    AgentId = agent.Id,
                    Title = $"Chat with {agent.Name}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();

                session.Messages = new List<ChatMessage>();
            }

            // Build message history for the AI call
            var history = session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new AIChatMessage(m.Role, m.Content))
                .ToList();
            history.Add(new AIChatMessage("user", body.Message));

            var aiRequest = new ChatRequest(
                agent.ModelName ?? "command-a-03-2025",
                agent.SystemPrompt ?? string.Empty,
                history
            );

            var aiResponse = await _aiChatService.SendMessageAsync(aiRequest);

            // Save user message
            var userMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = "user",
                Content = body.Message,
                CreatedAt = DateTime.UtcNow,
                TokensUsed = 0
            };
            _context.ChatMessages.Add(userMessage);

            // Save assistant reply
            var assistantMessage = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = "assistant",
                Content = aiResponse.Content ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                TokensUsed = aiResponse.TokensUsed
            };
            _context.ChatMessages.Add(assistantMessage);

            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                reply = aiResponse.Content,
                sessionId = session.Id,
                tokensUsed = aiResponse.TokensUsed
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}

public class AgentChatRequest
{
    public int AgentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? SessionId { get; set; }
}
