using ChatPortal.Models.Entities;

namespace ChatPortal.Services;

public interface IDataConnectionService
{
    Task<List<UserDataSource>> GetUserDataSourcesAsync(int userId);
    Task<UserDataSource?> GetDataSourceAsync(int id, int userId);
    Task<UserDataSource> CreateFileDataSourceAsync(int userId, string name, string sourceType, IFormFile file);
    Task<UserDataSource> CreateDatabaseDataSourceAsync(int userId, string name, string sourceType, string connectionString);
    Task<List<string>> GetAvailableTablesAsync(string sourceType, string connectionString);
    Task<UserDataSource> UpdateSelectedTablesAsync(int id, int userId, List<string> selectedTables);
    Task DeleteDataSourceAsync(int id, int userId);
    Task<List<Dictionary<string, object?>>> QueryDataSourceAsync(int dataSourceId, int userId, string query);
    Task<bool> ValidateConnectionAsync(string sourceType, string connectionString);
    Task<Dictionary<string, List<string>>> GetSchemaAsync(int dataSourceId, int userId);
    Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(int dataSourceId, int userId, string query);
}
