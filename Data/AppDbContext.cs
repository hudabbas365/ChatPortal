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
    public DbSet<Credit> Credits { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<AIPrediction> AIPredictions { get; set; }
    public DbSet<Addon> Addons { get; set; }
    public DbSet<UserAddon> UserAddons { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<Annotation> Annotations { get; set; }
    public DbSet<DataSource> DataSources { get; set; }
    public DbSet<Integration> Integrations { get; set; }
    public DbSet<Invite> Invites { get; set; }
    public DbSet<CaseStudy> CaseStudies { get; set; }
    public DbSet<Partner> Partners { get; set; }
    public DbSet<Recommendation> Recommendations { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<TrainingJob> TrainingJobs { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<Webhook> Webhooks { get; set; }
    public DbSet<SentimentResult> SentimentResults { get; set; }

    // New feature entities
    public DbSet<UserDataSource> UserDataSources { get; set; }
    public DbSet<CreditTransaction> CreditTransactions { get; set; }
    public DbSet<CreditPackage> CreditPackages { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    // Announcement system
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<AnnouncementReadStatus> AnnouncementReadStatuses { get; set; }

    // Workspace and feature management
    public DbSet<Workspace> Workspaces { get; set; }
    public DbSet<FeatureToggle> FeatureToggles { get; set; }

    // Organization management
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<Invitation> Invitations { get; set; }
    public DbSet<TeamWorkspacePermission> TeamWorkspacePermissions { get; set; }

    // Data Source Connections
    public DbSet<DataSourceConnection> DataSourceConnections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", Description = "Administrator" },
            new Role { Id = 2, Name = "User", Description = "Regular User" },
            new Role { Id = 3, Name = "Viewer", Description = "Read-only access to dashboards and reports" },
            new Role { Id = 4, Name = "Member", Description = "Access to chat, tasks, and shared workspaces" },
            new Role { Id = 5, Name = "Contributor", Description = "Can create and edit content in workspaces" },
            new Role { Id = 6, Name = "Super Admin", Description = "Full tenant-wide access including billing and feature toggles" }
        );

        modelBuilder.Entity<AIModel>().HasData(
            new AIModel { Id = 1, Name = "GPT-3.5 Turbo", Provider = "OpenAI", Version = "gpt-3.5-turbo", Description = "Fast and efficient model", IsActive = true, MaxTokens = 4096, CostPerToken = 0.000002m },
            new AIModel { Id = 2, Name = "GPT-4", Provider = "OpenAI", Version = "gpt-4", Description = "Most capable model", IsActive = true, MaxTokens = 8192, CostPerToken = 0.00003m }
        );

        modelBuilder.Entity<Plan>().HasData(
            new Plan { Id = 1, Name = "Free", Description = "Get started for free", MonthlyPrice = 0, AnnualPrice = 0, Features = "100 credits/month,1 AI model,Basic support", MaxCredits = 100, IsActive = true },
            new Plan { Id = 2, Name = "Pro", Description = "For professionals", MonthlyPrice = 19.99m, AnnualPrice = 199.99m, Features = "1000 credits/month,All AI models,Priority support,API access", MaxCredits = 1000, IsActive = true },
            new Plan { Id = 3, Name = "Enterprise", Description = "For teams and businesses", MonthlyPrice = 99.99m, AnnualPrice = 999.99m, Features = "Unlimited credits,All AI models,24/7 support,API access,Custom integrations,Team management", MaxCredits = int.MaxValue, IsActive = true }
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

        modelBuilder.Entity<CreditPackage>().HasData(
            new CreditPackage { Id = 1, Name = "Starter", Description = "100 AI query credits", Credits = 100, Price = 4.99m, IsActive = true },
            new CreditPackage { Id = 2, Name = "Standard", Description = "500 AI query credits", Credits = 500, Price = 19.99m, IsActive = true },
            new CreditPackage { Id = 3, Name = "Professional", Description = "2000 AI query credits", Credits = 2000, Price = 59.99m, IsActive = true }
        );

        // Demo users – IDs must match the in-memory UserService store (Id 1=Admin, Id 2=Demo)
        // Passwords hashed with SHA256 + static salt "ChatPortalSalt" (demo only)
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, FirstName = "Admin", LastName = "User", Email = "admin@chatportal.com", PasswordHash = "6F9C5BA6C2BBA8C601EC59FDE264D5D8245793A209E1034DA9D6BA9E35B11882", RoleId = 1, IsEmailVerified = true, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
            new User { Id = 2, FirstName = "Demo", LastName = "User", Email = "demo@chatportal.com", PasswordHash = "8EA9D894B93D6524C104DB00772D3F87AC40E55D958B37099C65036689F2E48D", RoleId = 2, IsEmailVerified = true, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
            new User { Id = 3, FirstName = "Alice", LastName = "Smith", Email = "alice@chatportal.com", PasswordHash = "1E80C5CB637E5E70B39D790F5C5BEFFD9F340C645342FD14DB3DCD06DC5468C5", RoleId = 2, IsEmailVerified = true, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
            new User { Id = 4, FirstName = "Bob", LastName = "Jones", Email = "bob@chatportal.com", PasswordHash = "1E80C5CB637E5E70B39D790F5C5BEFFD9F340C645342FD14DB3DCD06DC5468C5", RoleId = 2, IsEmailVerified = true, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate }
        );

        // Sample global announcements
        modelBuilder.Entity<Announcement>().HasData(
            new Announcement { Id = 1, Title = "Welcome to ChatPortal!", Content = "We're excited to have you on board. Explore AI-powered chat, data insights, and more.", Priority = AnnouncementPriority.Informational, CreatedByAdminId = 1, IsActive = true, CreatedAt = seedDate },
            new Announcement { Id = 2, Title = "Scheduled Maintenance – Jan 15", Content = "ChatPortal will be unavailable on January 15 from 02:00–04:00 UTC for scheduled maintenance.", Priority = AnnouncementPriority.Warning, CreatedByAdminId = 1, IsActive = true, CreatedAt = seedDate },
            new Announcement { Id = 3, Title = "Security Alert: Please Update Your Password", Content = "As part of our security improvements, we recommend updating your password immediately.", Priority = AnnouncementPriority.Urgent, CreatedByAdminId = 1, IsActive = true, CreatedAt = seedDate }
        );

        // Sample notifications for demo users
        modelBuilder.Entity<Notification>().HasData(
            new Notification { Id = 1, UserId = 2, Title = "Welcome to ChatPortal!", Content = "Your account is ready. Start chatting with AI models today.", Priority = NotificationPriority.Informational, IsRead = false, IsDismissed = false, CreatedAt = seedDate },
            new Notification { Id = 2, UserId = 2, Title = "Try the Data Insights feature", Content = "Connect your CSV or database and ask AI questions about your data.", Priority = NotificationPriority.Informational, IsRead = false, IsDismissed = false, CreatedAt = seedDate },
            new Notification { Id = 3, UserId = 1, Title = "Admin: New user registered", Content = "Demo User (demo@chatportal.com) has joined ChatPortal.", Priority = NotificationPriority.Informational, IsRead = false, IsDismissed = false, CreatedAt = seedDate }
        );
    }
}
