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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin", Description = "Administrator" },
            new Role { Id = 2, Name = "User", Description = "Regular User" }
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

        modelBuilder.Entity<CreditPackage>().HasData(
            new CreditPackage { Id = 1, Name = "Starter", Description = "100 AI query credits", Credits = 100, Price = 4.99m, IsActive = true },
            new CreditPackage { Id = 2, Name = "Standard", Description = "500 AI query credits", Credits = 500, Price = 19.99m, IsActive = true },
            new CreditPackage { Id = 3, Name = "Professional", Description = "2000 AI query credits", Credits = 2000, Price = 59.99m, IsActive = true }
        );
    }
}
