using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class OrganizationController : Controller
{
    private readonly AppDbContext _context;

    public OrganizationController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // GET: Organization/Index
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var organizations = await _context.Organizations
            .Include(o => o.Members)
            .Where(o => o.OwnerId == userId || o.Members.Any(m => m.UserId == userId && m.IsActive))
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Description,
                o.Industry,
                IsOwner = o.OwnerId == userId,
                MemberCount = o.Members.Count(m => m.IsActive) + 1,
                WorkspaceCount = o.Workspaces.Count,
                o.CreatedAt
            })
            .ToListAsync();

        return View(organizations);
    }

    // POST: Organization/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description, string? industry, string? phone, string? website)
    {
        try
        {
            var userId = GetUserId();

            // Check if user already owns an organization
            var existingOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.OwnerId == userId);

            if (existingOrg != null)
                return Json(new { success = false, error = "You already have an organization. Each user can only create one organization." });

            var organization = new Organization
            {
                Name = name,
                Description = description,
                Industry = industry,
                Phone = phone,
                Website = website,
                OwnerId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Automatically set as active organization
            HttpContext.Session.SetInt32("ActiveOrganizationId", organization.Id);

            return Json(new { success = true, organizationId = organization.Id, message = "Organization created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: Organization/GetUserOrganizations
    [HttpGet]
    public async Task<IActionResult> GetUserOrganizations()
    {
        var userId = GetUserId();
        var organizations = await _context.Organizations
            .Include(o => o.Members)
            .Include(o => o.Workspaces)
            .Where(o => o.OwnerId == userId || o.Members.Any(m => m.UserId == userId && m.IsActive))
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Description,
                o.Industry,
                IsOwner = o.OwnerId == userId,
                MemberCount = o.Members.Count(m => m.IsActive) + 1,
                WorkspaceCount = o.Workspaces.Count,
                UserRole = o.OwnerId == userId ? "Owner" : 
                    o.Members.Where(m => m.UserId == userId && m.IsActive).Select(m => m.Role).FirstOrDefault()
            })
            .ToListAsync();

        return Json(new { success = true, organizations });
    }

    // POST: Organization/AddMember
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(int organizationId, string email, string role = "Member")
    {
        try
        {
            var userId = GetUserId();
            var organization = await _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
                return Json(new { success = false, error = "Organization not found" });

            // Check if user is owner or admin
            var isOwner = organization.OwnerId == userId;
            var isAdmin = organization.Members.Any(m => m.UserId == userId && m.Role == "Admin" && m.IsActive);

            if (!isOwner && !isAdmin)
                return Json(new { success = false, error = "Access denied. Only owners and admins can add members" });

            var inviteUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (inviteUser == null)
                return Json(new { success = false, error = "User not found" });

            if (organization.OwnerId == inviteUser.Id)
                return Json(new { success = false, error = "User is already the owner" });

            if (organization.Members.Any(m => m.UserId == inviteUser.Id && m.IsActive))
                return Json(new { success = false, error = "User is already a member" });

            var member = new OrganizationMember
            {
                OrganizationId = organizationId,
                UserId = inviteUser.Id,
                Role = role,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };

            _context.OrganizationMembers.Add(member);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Member added successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Organization/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? description, string? industry, string? phone, string? website)
    {
        try
        {
            var userId = GetUserId();
            var organization = await _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
                return Json(new { success = false, error = "Organization not found" });

            // Check if user is owner or admin
            var isOwner = organization.OwnerId == userId;
            var isAdmin = organization.Members.Any(m => m.UserId == userId && m.Role == "Admin" && m.IsActive);

            if (!isOwner && !isAdmin)
                return Json(new { success = false, error = "Access denied" });

            organization.Name = name;
            organization.Description = description;
            organization.Industry = industry;
            organization.Phone = phone;
            organization.Website = website;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Organization updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Organization/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == id && o.OwnerId == userId);

            if (organization == null)
                return Json(new { success = false, error = "Organization not found or access denied. Only owners can delete organizations." });

            _context.Organizations.Remove(organization);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Organization deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Organization/SetActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id)
    {
        try
        {
            var userId = GetUserId();
            var organization = await _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == id && 
                    (o.OwnerId == userId || o.Members.Any(m => m.UserId == userId && m.IsActive)));

            if (organization == null)
                return Json(new { success = false, error = "Organization not found or access denied" });

            // Store in session
            HttpContext.Session.SetInt32("ActiveOrganizationId", id);

            return Json(new { success = true, organizationName = organization.Name });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: Organization/GetMembers
    [HttpGet]
    public async Task<IActionResult> GetMembers(int organizationId)
    {
        try
        {
            var userId = GetUserId();
            var organization = await _context.Organizations
                .Include(o => o.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
                return Json(new { success = false, error = "Organization not found" });

            // Check if user has access to this organization
            var hasAccess = organization.OwnerId == userId || 
                           organization.Members.Any(m => m.UserId == userId && m.IsActive);

            if (!hasAccess)
                return Json(new { success = false, error = "Access denied" });

            var members = organization.Members
                .Where(m => m.IsActive)
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

    // POST: Organization/UpdateMemberRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMemberRole(int organizationId, int userId, string role)
    {
        try
        {
            var currentUserId = GetUserId();
            var organization = await _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
                return Json(new { success = false, error = "Organization not found" });

            // Check if current user is owner or admin
            var isOwner = organization.OwnerId == currentUserId;
            var isAdmin = organization.Members.Any(m => m.UserId == currentUserId && m.Role == "Admin" && m.IsActive);

            if (!isOwner && !isAdmin)
                return Json(new { success = false, error = "Access denied. Only owners and admins can update member roles" });

            // Find the member to update
            var member = organization.Members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
            if (member == null)
                return Json(new { success = false, error = "Member not found" });

            // Prevent changing owner's role
            if (userId == organization.OwnerId)
                return Json(new { success = false, error = "Cannot change the owner's role" });

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

    // POST: Organization/RemoveMember
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int organizationId, int userId)
    {
        try
        {
            var currentUserId = GetUserId();
            var organization = await _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
                return Json(new { success = false, error = "Organization not found" });

            // Check if current user is owner or admin
            var isOwner = organization.OwnerId == currentUserId;
            var isAdmin = organization.Members.Any(m => m.UserId == currentUserId && m.Role == "Admin" && m.IsActive);

            if (!isOwner && !isAdmin)
                return Json(new { success = false, error = "Access denied. Only owners and admins can remove members" });

            // Find the member to remove
            var member = organization.Members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
            if (member == null)
                return Json(new { success = false, error = "Member not found" });

            // Prevent removing the owner
            if (userId == organization.OwnerId)
                return Json(new { success = false, error = "Cannot remove the owner from the organization" });

            // Deactivate the member
            member.IsActive = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Member removed successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }
}
