using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class TeamController : Controller
{
    private readonly AppDbContext _context;

    public TeamController(AppDbContext context)
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

    // GET: Team/Index
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var teams = await _context.Teams
            .Include(t => t.Members)
            .Where(t => t.OwnerId == userId || t.Members.Any(m => m.UserId == userId))
            .ToListAsync();

        return View(teams);
    }

    // POST: Team/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description)
    {
        try
        {
            var userId = GetUserId();
            var orgId = await GetActiveOrganizationIdAsync();

            if (!orgId.HasValue)
                return Json(new { success = false, error = "No active organization. Please create or select an organization first." });

            // Verify user has permission in this organization
            var org = await _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == orgId.Value);

            if (org == null)
                return Json(new { success = false, error = "Organization not found" });

            var isOwner = org.OwnerId == userId;
            var isAdmin = org.Members.Any(m => m.UserId == userId && m.Role == "Admin");

            if (!isOwner && !isAdmin)
                return Json(new { success = false, error = "Only organization owners and admins can create teams" });

            var team = new Team
            {
                Name = name,
                Description = description,
                OrganizationId = orgId.Value,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            return Json(new { success = true, teamId = team.Id, message = "Team created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: Team/GetUserTeams
    [HttpGet]
    public async Task<IActionResult> GetUserTeams()
    {
        var userId = GetUserId();
        var orgId = await GetActiveOrganizationIdAsync();

        if (!orgId.HasValue)
            return Json(new { success = true, teams = new List<object>() });

        var teams = await _context.Teams
            .Include(t => t.Members)
            .Where(t => t.OrganizationId == orgId.Value && 
                       (t.OwnerId == userId || t.Members.Any(m => m.UserId == userId)))
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                MemberCount = t.Members.Count + 1,
                IsOwner = t.OwnerId == userId
            })
            .ToListAsync();

        return Json(new { success = true, teams });
    }

    // POST: Team/AddMember
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(int teamId, string email, string role = "Member")
    {
        try
        {
            var userId = GetUserId();
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId && t.OwnerId == userId);

            if (team == null)
                return Json(new { success = false, error = "Team not found or access denied" });

            var inviteUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (inviteUser == null)
                return Json(new { success = false, error = "User not found" });

            if (team.Members.Any(m => m.UserId == inviteUser.Id))
                return Json(new { success = false, error = "User is already a member" });

            var member = new TeamMember
            {
                TeamId = teamId,
                UserId = inviteUser.Id,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };

            _context.TeamMembers.Add(member);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Member added successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Team/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId);

            if (team == null)
                return Json(new { success = false, error = "Team not found or access denied" });

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Team deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Team/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? description)
    {
        try
        {
            var userId = GetUserId();
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId);

            if (team == null)
                return Json(new { success = false, error = "Team not found or access denied" });

            team.Name = name;
            team.Description = description;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Team updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Team/GrantWorkspaceAccess
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantWorkspaceAccess(int teamId, int workspaceId, string permission = "View")
    {
        try
        {
            var userId = GetUserId();

            // Verify team ownership
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId && t.OwnerId == userId);

            if (team == null)
                return Json(new { success = false, error = "Team not found or access denied" });

            // Verify workspace belongs to same organization
            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == workspaceId && w.OrganizationId == team.OrganizationId);

            if (workspace == null)
                return Json(new { success = false, error = "Workspace not found or not in same organization" });

            // Check if permission already exists
            var existing = await _context.TeamWorkspacePermissions
                .FirstOrDefaultAsync(p => p.TeamId == teamId && p.WorkspaceId == workspaceId);

            if (existing != null)
            {
                existing.Permission = permission;
            }
            else
            {
                var newPermission = new TeamWorkspacePermission
                {
                    TeamId = teamId,
                    WorkspaceId = workspaceId,
                    Permission = permission,
                    GrantedBy = userId,
                    GrantedAt = DateTime.UtcNow
                };
                _context.TeamWorkspacePermissions.Add(newPermission);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Workspace access granted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Team/RevokeWorkspaceAccess
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeWorkspaceAccess(int teamId, int workspaceId)
    {
        try
        {
            var userId = GetUserId();

            // Verify team ownership
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId && t.OwnerId == userId);

            if (team == null)
                return Json(new { success = false, error = "Team not found or access denied" });

            var permission = await _context.TeamWorkspacePermissions
                .FirstOrDefaultAsync(p => p.TeamId == teamId && p.WorkspaceId == workspaceId);

            if (permission != null)
            {
                _context.TeamWorkspacePermissions.Remove(permission);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Workspace access revoked successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: Team/GetWorkspacePermissions
    [HttpGet]
    public async Task<IActionResult> GetWorkspacePermissions(int teamId)
    {
        var userId = GetUserId();

        // Verify access to team
        var team = await _context.Teams
            .Include(t => t.WorkspacePermissions)
            .ThenInclude(p => p.Workspace)
            .FirstOrDefaultAsync(t => t.Id == teamId && 
                (t.OwnerId == userId || t.Members.Any(m => m.UserId == userId)));

        if (team == null)
            return Json(new { success = false, error = "Team not found or access denied" });

        var permissions = team.WorkspacePermissions.Select(p => new
        {
            workspaceId = p.WorkspaceId,
            workspaceName = p.Workspace.Name,
            permission = p.Permission,
            grantedAt = p.GrantedAt.ToString("yyyy-MM-dd")
        }).ToList();

        return Json(new { success = true, permissions });
    }

    // GET: Team/GetMembers
    [HttpGet]
    public async Task<IActionResult> GetMembers(int teamId)
    {
        try
        {
            var userId = GetUserId();
            var team = await _context.Teams
                .Include(t => t.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return Json(new { success = false, error = "Team not found" });

            // Check if user has access to this team
            var hasAccess = team.OwnerId == userId || 
                           team.Members.Any(m => m.UserId == userId);

            if (!hasAccess)
                return Json(new { success = false, error = "Access denied" });

            var members = team.Members
                .Select(m => new
                {
                    userId = m.UserId,
                    name = m.User.FirstName + " " + m.User.LastName,
                    email = m.User.Email,
                    role = m.Role,
                    joinedAt = m.JoinedAt
                })
                .ToList();

            return Json(new { success = true, members });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Team/UpdateMemberRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMemberRole(int teamId, int userId, string role)
    {
        try
        {
            var currentUserId = GetUserId();
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return Json(new { success = false, error = "Team not found" });

            // Check if current user is team owner
            if (team.OwnerId != currentUserId)
                return Json(new { success = false, error = "Access denied. Only team owners can update member roles" });

            // Find the member to update
            var member = team.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
                return Json(new { success = false, error = "Member not found" });

            // Prevent changing owner's role
            if (userId == team.OwnerId)
                return Json(new { success = false, error = "Cannot change the team owner's role" });

            // Update the role
            member.Role = role;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Member role updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: Team/SearchUsers?q=...
    [HttpGet]
    public async Task<IActionResult> SearchUsers(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(new { success = true, users = new List<object>() });

        var orgId = await GetActiveOrganizationIdAsync();
        var lower = q.ToLower();

        var users = await _context.Users
            .Where(u => u.Email.ToLower().Contains(lower) ||
                        u.FirstName.ToLower().Contains(lower) ||
                        u.LastName.ToLower().Contains(lower))
            .Take(10)
            .Select(u => new
            {
                id = u.Id,
                name = u.FirstName + " " + u.LastName,
                email = u.Email,
                isInOrganization = orgId.HasValue &&
                    _context.OrganizationMembers.Any(m => m.OrganizationId == orgId.Value && m.UserId == u.Id)
            })
            .ToListAsync();

        if (!users.Any())
            return Json(new { success = true, users = new List<object>(), hint = "User not found. An invitation email will be sent." });

        return Json(new { success = true, users });
    }

    // POST: Team/RemoveMember
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int teamId, int userId)
    {
        try
        {
            var currentUserId = GetUserId();
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
                return Json(new { success = false, error = "Team not found" });

            // Check if current user is team owner
            if (team.OwnerId != currentUserId)
                return Json(new { success = false, error = "Access denied. Only team owners can remove members" });

            // Find the member to remove
            var member = team.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
                return Json(new { success = false, error = "Member not found" });

            // Prevent removing the owner
            if (userId == team.OwnerId)
                return Json(new { success = false, error = "Cannot remove the team owner" });

            // Remove the member
            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Member removed successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}
