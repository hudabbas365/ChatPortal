using ChatPortal.Models.Entities;
using System.Data;

namespace ChatPortal.Services.DataSourceConnectors
{
    public interface IDataSourceConnector
    {
        Task<ConnectionResult> TestConnectionAsync(DataSourceConnection connection);
        Task<ConnectionResult> ConnectAsync(DataSourceConnection connection);
        Task<bool> DisconnectAsync(int connectionId);
        Task<SyncResult> SyncDataAsync(int connectionId);
        Task<HealthStatus> GetHealthAsync(int connectionId);
        string GetConfigurationSchema();

        // New methods for query execution
        Task<List<string>> GetTablesAsync(DataSourceConnection connection);
        Task<Dictionary<string, string>> GetTableSchemaAsync(DataSourceConnection connection, string tableName);
        Task<DataTable> ExecuteQueryAsync(DataSourceConnection connection, string query);
    }

    public class ConnectionResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorDetails { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class SyncResult
    {
        public bool Success { get; set; }
        public int RecordsProcessed { get; set; }
        public DateTime SyncTime { get; set; }
        public string? Message { get; set; }
        public string? ErrorDetails { get; set; }
    }

    public class HealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty; // "Connected", "Disconnected", "Error", "Warning"
        public DateTime LastChecked { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, object>? Details { get; set; }
    }
}
