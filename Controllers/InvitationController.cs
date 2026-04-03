using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatPortal.Controllers;

[Authorize]
public class InvitationController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public InvitationController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // POST: Invitation/SendOrganizationInvite
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendOrganizationInvite(int organizationId, string email, string role = "Member")
    {
        try
        {
            var userId = GetUserId();

            // Verify user is organization owner/admin
            var org = await _context.Organizations
                .Include(o => o.Members)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org == null)
                return Json(new { success = false, error = "Organization not found" });

            var isOwner = org.OwnerId == userId;
            var isAdmin = org.Members.Any(m => m.UserId == userId && m.Role == "Admin");

            if (!isOwner && !isAdmin)
                return Json(new { success = false, error = "Only organization owners and admins can send invitations" });

            // Check if user exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
            {
                // User exists - check if already a member
                var isMember = org.OwnerId == existingUser.Id || 
                               org.Members.Any(m => m.UserId == existingUser.Id);

                if (isMember)
                    return Json(new { success = false, error = "User is already a member of this organization" });

                // Add directly as member
                var member = new OrganizationMember
                {
                    OrganizationId = organizationId,
                    UserId = existingUser.Id,
                    Role = role,
                    JoinedAt = DateTime.UtcNow
                };

                _context.OrganizationMembers.Add(member);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"User added to organization successfully",
                    userExists = true
                });
            }
            else
            {
                // User doesn't exist - create invitation
                var token = GenerateInvitationToken();
                var invitation = new Invitation
                {
                    Email = email,
                    OrganizationId = organizationId,
                    Role = role,
                    InvitedBy = userId,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Invitations.Add(invitation);
                await _context.SaveChangesAsync();

                // Generate invitation link
                var inviteLink = $"{Request.Scheme}://{Request.Host}/Account/Register?inviteToken={token}";

                // TODO: Send email with invitation link
                // await _emailService.SendInvitationEmail(email, org.Name, inviteLink);

                return Json(new { 
                    success = true, 
                    message = $"Invitation sent to {email}",
                    inviteLink = inviteLink,
                    userExists = false
                });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: Invitation/SendTeamInvite
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTeamInvite(int teamId, string email, string role = "Member")
    {
        try
        {
            var userId = GetUserId();

            // Verify user is team owner
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId && t.OwnerId == userId);

            if (team == null)
                return Json(new { success = false, error = "Team not found or access denied" });

            // Check if user exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
            {
                // User exists - check if already a member
                if (team.Members.Any(m => m.UserId == existingUser.Id))
                    return Json(new { success = false, error = "User is already a team member" });

                // Add directly as member
                var member = new TeamMember
                {
                    TeamId = teamId,
                    UserId = existingUser.Id,
                    Role = role,
                    JoinedAt = DateTime.UtcNow
                };

                _context.TeamMembers.Add(member);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"User added to team successfully",
                    userExists = true
                });
            }
            else
            {
                // User doesn't exist - create invitation
                var token = GenerateInvitationToken();
                var invitation = new Invitation
                {
                    Email = email,
                    TeamId = teamId,
                    Role = role,
                    InvitedBy = userId,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Invitations.Add(invitation);
                await _context.SaveChangesAsync();

                // Generate invitation link
                var inviteLink = $"{Request.Scheme}://{Request.Host}/Account/Register?inviteToken={token}";

                // TODO: Send email with invitation link
                // await _emailService.SendInvitationEmail(email, team.Name, inviteLink);

                return Json(new { 
                    success = true, 
                    message = $"Invitation sent to {email}",
                    inviteLink = inviteLink,
                    userExists = false
                });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: Invitation/AcceptInvitation
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitation(string token)
    {
        var invitation = await _context.Invitations
            .Include(i => i.Organization)
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsAccepted && i.ExpiresAt > DateTime.UtcNow);

        if (invitation == null)
            return View("InvitationExpired");

        return View(invitation);
    }

    private string GenerateInvitationToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
