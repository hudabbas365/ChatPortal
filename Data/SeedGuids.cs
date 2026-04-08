namespace ChatPortal.Data;

/// <summary>
/// Deterministic Guid constants for all seed data, ensuring reproducible HasData() calls
/// across migrations and environments.
/// </summary>
public static class SeedGuids
{
    // Roles
    public static readonly Guid AdminRole       = new("10000000-0000-0000-0000-000000000001");
    public static readonly Guid UserRole        = new("10000000-0000-0000-0000-000000000002");
    public static readonly Guid ViewerRole      = new("10000000-0000-0000-0000-000000000003");
    public static readonly Guid MemberRole      = new("10000000-0000-0000-0000-000000000004");
    public static readonly Guid ContributorRole = new("10000000-0000-0000-0000-000000000005");
    public static readonly Guid SuperAdminRole  = new("10000000-0000-0000-0000-000000000006");

    // AI Models
    public static readonly Guid Gpt35Turbo = new("20000000-0000-0000-0000-000000000001");
    public static readonly Guid Gpt4       = new("20000000-0000-0000-0000-000000000002");

    // Plans
    public static readonly Guid FreePlan       = new("30000000-0000-0000-0000-000000000001");
    public static readonly Guid ProPlan        = new("30000000-0000-0000-0000-000000000002");
    public static readonly Guid EnterprisePlan = new("30000000-0000-0000-0000-000000000003");

    // Default Users
    public static readonly Guid SuperAdminUserId = new("40000000-0000-0000-0000-000000000001");
    public static readonly Guid AdminUserId      = new("40000000-0000-0000-0000-000000000002");
    public static readonly Guid DemoUserId       = new("40000000-0000-0000-0000-000000000003");
    public static readonly Guid AliceUserId      = new("40000000-0000-0000-0000-000000000004");
    public static readonly Guid BobUserId        = new("40000000-0000-0000-0000-000000000005");

    // Default Organizations
    public static readonly Guid SystemOrgId = new("50000000-0000-0000-0000-000000000001");
    public static readonly Guid DemoOrgId   = new("50000000-0000-0000-0000-000000000002");
}
