using ChatPortal.Models.Entities;

namespace ChatPortal.ViewModels;

public class DataConnectionViewModel
{
    public List<UserDataSource> DataSources { get; set; } = new();
    public int CreditBalance { get; set; }
}

public class CreateFileDataSourceViewModel
{
    public string Name { get; set; } = string.Empty;
    public string SourceType { get; set; } = "Excel";
    public IFormFile? File { get; set; }
}

public class CreateDbDataSourceViewModel
{
    public string Name { get; set; } = string.Empty;
    public string SourceType { get; set; } = "SqlServer";
    public string ConnectionString { get; set; } = string.Empty;
}

public class PaymentViewModel
{
    public List<CreditPackage> Packages { get; set; } = new();
    public int CurrentBalance { get; set; }
    public List<PaymentTransaction> RecentTransactions { get; set; } = new();
    public string? PublishableKey { get; set; }
    public string? PayPalClientId { get; set; }
}

public class DataInsightsViewModel
{
    public UserDataSource DataSource { get; set; } = null!;
    public int CreditBalance { get; set; }
}
