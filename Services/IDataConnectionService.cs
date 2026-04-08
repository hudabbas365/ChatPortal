using ChatPortal.Models.Entities;

namespace ChatPortal.Services;

public interface IDataConnectionService
{
    Task<List<UserDataSource>> GetUserDataSourcesAsync(Guid userId);
    Task<UserDataSource?> GetDataSourceAsync(Guid id, Guid userId);
    Task<UserDataSource> CreateFileDataSourceAsync(Guid userId, string name, string sourceType, IFormFile file);
    Task<UserDataSource> CreateDatabaseDataSourceAsync(Guid userId, string name, string sourceType, string connectionString);
    Task<List<string>> GetAvailableTablesAsync(string sourceType, string connectionString);
    Task<UserDataSource> UpdateSelectedTablesAsync(Guid id, Guid userId, List<string> selectedTables);
    Task DeleteDataSourceAsync(Guid id, Guid userId);
    Task<List<Dictionary<string, object?>>> QueryDataSourceAsync(Guid dataSourceId, Guid userId, string query);
    Task<bool> ValidateConnectionAsync(string sourceType, string connectionString);
    Task<Dictionary<string, List<string>>> GetSchemaAsync(Guid dataSourceId, Guid userId);
    Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(Guid dataSourceId, Guid userId, string query);
}
