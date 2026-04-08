using ChatPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatPortal.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<AIModel> AIModels { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<Annotation> Annotations { get; set; }
    public DbSet<DataSource> DataSources { get; set; }
    public DbSet<DataSourceConnection> DataSourceConnections { get; set; }
    public DbSet<QueryHistory> QueryHistories { get; set; }
    public DbSet<ChartDefinition> ChartDefinitions { get; set; }
    public DbSet<Dashboard> Dashboards { get; set; }
    public DbSet<PinnedChart> PinnedCharts { get; set; }
    public DbSet<Integration> Integrations { get; set; }
    public DbSet<Invite> Invites { get; set; }
    public DbSet<CaseStudy> CaseStudies { get; set; }
    public DbSet<Partner> Partners { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Webhook> Webhooks { get; set; }

    // Feature entities
    public DbSet<UserDataSource> UserDataSources { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    // Announcement system
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<AnnouncementReadStatus> AnnouncementReadStatuses { get; set; }

    // Workspace and feature management
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<FeatureToggle> FeatureToggles { get; set; }

    // Organization system
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<Invitation> Invitations { get; set; }

    // Agent system
    public DbSet<Agent> Agents { get; set; }

    // Team workspace permissions
    public DbSet<TeamWorkspacePermission> TeamWorkspacePermissions { get; set; }

    // Error logging
    public DbSet<ErrorLog> ErrorLogs { get; set; }

    // New entities
    public DbSet<EmbedToken> EmbedTokens { get; set; }
    public DbSet<PushedChartDataset> PushedChartDatasets { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = SeedGuids.AdminRole,       Name = "Admin",       Description = "Administrator" },
            new Role { Id = SeedGuids.UserRole,        Name = "User",        Description = "Regular User" },
            new Role { Id = SeedGuids.ViewerRole,      Name = "Viewer",      Description = "Read-only access to dashboards and reports" },
            new Role { Id = SeedGuids.MemberRole,      Name = "Member",      Description = "Access to chat, tasks, and shared workspaces" },
            new Role { Id = SeedGuids.ContributorRole, Name = "Contributor", Description = "Can create and edit content in workspaces" },
            new Role { Id = SeedGuids.SuperAdminRole,  Name = "Super Admin", Description = "Full tenant-wide access including billing and feature toggles" }
        );

        modelBuilder.Entity<AIModel>().HasData(
            new AIModel { Id = SeedGuids.Gpt35Turbo, Name = "GPT-3.5 Turbo", Provider = "OpenAI", Version = "gpt-3.5-turbo", Description = "Fast and efficient model", IsActive = true, MaxTokens = 4096, CostPerToken = 0.000002m },
            new AIModel { Id = SeedGuids.Gpt4,       Name = "GPT-4",         Provider = "OpenAI", Version = "gpt-4",          Description = "Most capable model",     IsActive = true, MaxTokens = 8192, CostPerToken = 0.00003m }
        );

        modelBuilder.Entity<Plan>().HasData(
            new Plan { Id = SeedGuids.FreePlan,       Name = "Free Trial",  Description = "30 days free access", MonthlyPrice = 0,    AnnualPrice = 0, MaxWorkspaces = 3,  MaxCharts = 10, Features = "3 workspaces,10 charts,Basic support",                                                                       IsActive = true },
            new Plan { Id = SeedGuids.ProPlan,        Name = "Pro",         Description = "$20/month",           MonthlyPrice = 20m,  AnnualPrice = 0, MaxWorkspaces = 10, MaxCharts = 50, Features = "10 workspaces,50 charts,Priority support,API access",                                                          IsActive = true },
            new Plan { Id = SeedGuids.EnterprisePlan, Name = "Enterprise",  Description = "$45/month",           MonthlyPrice = 45m,  AnnualPrice = 0, MaxWorkspaces = -1, MaxCharts = -1, Features = "Unlimited workspaces,Unlimited charts,24/7 support,Custom integrations,Dedicated account manager",            IsActive = true }
        );

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeamMember>()
            .HasOne(tm => tm.User)
            .WithMany()
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invite>()
            .HasOne(i => i.InvitedBy)
            .WithMany()
            .HasForeignKey(i => i.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AnnouncementReadStatus>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(a => a.CreatedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Workspace>()
            .HasOne(w => w.Owner)
            .WithMany()
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Workspace>()
            .HasIndex(w => new { w.OrganizationId, w.Name })
            .IsUnique();

        modelBuilder.Entity<Organization>()
            .HasOne(o => o.Owner)
            .WithMany()
            .HasForeignKey(o => o.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrganizationMember>()
            .HasOne(om => om.User)
            .WithMany()
            .HasForeignKey(om => om.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Agent>()
            .HasOne(a => a.Creator)
            .WithMany()
            .HasForeignKey(a => a.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Team>()
            .HasOne(t => t.Organization)
            .WithMany(o => o.Teams)
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.Inviter)
            .WithMany()
            .HasForeignKey(i => i.InvitedBy)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TeamWorkspacePermission>()
            .HasOne(twp => twp.Granter)
            .WithMany()
            .HasForeignKey(twp => twp.GrantedBy)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<FeatureToggle>()
            .HasOne(f => f.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(f => f.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QueryHistory>()
            .HasOne(q => q.DataSource)
            .WithMany()
            .HasForeignKey(q => q.DataSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PinnedChart>()
            .HasOne(p => p.Dashboard)
            .WithMany(d => d.PinnedCharts)
            .HasForeignKey(p => p.DashboardId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<Dashboard>()
            .HasIndex(d => d.PublicSlug)
            .IsUnique();

        modelBuilder.Entity<EmbedToken>()
            .HasOne(e => e.Dashboard)
            .WithMany()
            .HasForeignKey(e => e.DashboardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmbedToken>()
            .HasOne(e => e.Creator)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PushedChartDataset>()
            .HasOne(p => p.Organization)
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PushedChartDataset>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.ActorUser)
            .WithMany()
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>().HasData(
            new User { Id = SeedGuids.SuperAdminUserId, FirstName = "Super",  LastName = "Admin",  Email = "superadmin@chatportal.com", PasswordHash = "6F9C5BA6C2BBA8C601EC59FDE264D5D8245793A209E1034DA9D6BA9E35B11882", RoleId = SeedGuids.SuperAdminRole, IsEmailVerified = true, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
            new User { Id = SeedGuids.AdminUserId,      FirstName = "Admin",  LastName = "User",   Email = "admin@chatportal.com",      PasswordHash = "6F9C5BA6C2BBA8C601EC59FDE264D5D8245793A209E1034DA9D6BA9E35B11882", RoleId = SeedGuids.AdminRole,      IsEmailVerified = true, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
            new User { Id = SeedGuids.DemoUserId,       FirstName = "Demo",   LastName = "User",   Email = "demo@chatportal.com",       PasswordHash = "8EA9D894B93D6524C104DB00772D3F87AC40E55D958B37099C65036689F2E48D", RoleId = SeedGuids.UserRole,       IsEmailVerified = true, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
            new User { Id = SeedGuids.AliceUserId,      FirstName = "Alice",  LastName = "Smith",  Email = "alice@chatportal.com",      PasswordHash = "1E80C5CB637E5E70B39D790F5C5BEFFD9F340C645342FD14DB3DCD06DC5468C5", RoleId = SeedGuids.UserRole,       IsEmailVerified = true, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
            new User { Id = SeedGuids.BobUserId,        FirstName = "Bob",    LastName = "Jones",  Email = "bob@chatportal.com",        PasswordHash = "1E80C5CB637E5E70B39D790F5C5BEFFD9F340C645342FD14DB3DCD06DC5468C5", RoleId = SeedGuids.UserRole,       IsEmailVerified = true, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate }
        );

        modelBuilder.Entity<Announcement>().HasData(
            new Announcement { Id = new Guid("60000000-0000-0000-0000-000000000001"), Title = "Welcome to ChatPortal!", Content = "We're excited to have you on board. Explore AI-powered chat, data insights, and more.", Priority = AnnouncementPriority.Informational, CreatedByAdminId = SeedGuids.AdminUserId, IsActive = true, CreatedAt = seedDate },
            new Announcement { Id = new Guid("60000000-0000-0000-0000-000000000002"), Title = "Scheduled Maintenance – Jan 15", Content = "ChatPortal will be unavailable on January 15 from 02:00–04:00 UTC for scheduled maintenance.", Priority = AnnouncementPriority.Warning, CreatedByAdminId = SeedGuids.AdminUserId, IsActive = true, CreatedAt = seedDate }
        );

        modelBuilder.Entity<Notification>().HasData(
            new Notification { Id = new Guid("70000000-0000-0000-0000-000000000001"), UserId = SeedGuids.DemoUserId, Title = "Welcome to ChatPortal!", Content = "Your account is ready. Start chatting with AI models today.", Priority = NotificationPriority.Informational, IsRead = false, IsDismissed = false, CreatedAt = seedDate },
            new Notification { Id = new Guid("70000000-0000-0000-0000-000000000002"), UserId = SeedGuids.AdminUserId, Title = "Admin: New user registered", Content = "Demo User (demo@chatportal.com) has joined ChatPortal.", Priority = NotificationPriority.Informational, IsRead = false, IsDismissed = false, CreatedAt = seedDate }
        );
    }
}
