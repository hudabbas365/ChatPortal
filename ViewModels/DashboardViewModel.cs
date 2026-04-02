namespace ChatPortal.ViewModels;

public class DashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public int TotalChats { get; set; }
    public int CreditsUsed { get; set; }
    public int CreditsRemaining { get; set; }
    public string PlanName { get; set; } = "Free";
    public List<RecentActivityItem> RecentActivity { get; set; } = new();
    public List<QuickStatItem> Stats { get; set; } = new();
}

public class RecentActivityItem
{
    public string Action { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-activity";
}

public class QuickStatItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = "primary";
}
