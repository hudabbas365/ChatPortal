using ChatPortal.Models.Entities;
using ChatPortal.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;

namespace ChatPortal.Services.DataSourceConnectors.SQLConnectors
{
    public class MySQLConnector : IDataSourceConnector
    {
        private readonly AppDbContext _context;

        public MySQLConnector(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ConnectionResult> TestConnectionAsync(DataSourceConnection connection)
        {
            try
            {
                using var conn = new MySqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                if (conn.State == ConnectionState.Open)
                {
                    var version = conn.ServerVersion;
                    var database = conn.Database;
                    await conn.CloseAsync();

                    return new ConnectionResult
                    {
                        Success = true,
                        Message = "Successfully connected to MySQL database",
                        Metadata = new Dictionary<string, object>
                        {
                            { "ServerVersion", version },
                            { "Database", database }
                        }
                    };
                }

                return new ConnectionResult
                {
                    Success = false,
                    Message = "Failed to open MySQL connection"
                };
            }
            catch (Exception ex)
            {
                return new ConnectionResult
                {
                    Success = false,
                    Message = "MySQL connection test failed",
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<ConnectionResult> ConnectAsync(DataSourceConnection connection)
        {
            var testResult = await TestConnectionAsync(connection);

            if (testResult.Success)
            {
                connection.LastSyncAt = DateTime.UtcNow;
                connection.LastSyncStatus = "Connected";
                connection.IsActive = true;

                _context.DataSourceConnections.Update(connection);
                await _context.SaveChangesAsync();
            }

            return testResult;
        }

        public async Task<bool> DisconnectAsync(int connectionId)
        {
            var connection = await _context.DataSourceConnections.FindAsync(connectionId);
            if (connection == null) return false;

            connection.IsActive = false;
            connection.LastSyncStatus = "Disconnected";
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<SyncResult> SyncDataAsync(int connectionId)
        {
            var connection = await _context.DataSourceConnections.FindAsync(connectionId);
            if (connection == null)
            {
                return new SyncResult
                {
                    Success = false,
                    Message = "Connection not found",
                    SyncTime = DateTime.UtcNow
                };
            }

            try
            {
                using var conn = new MySqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand("SHOW TABLES", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                int tableCount = 0;
                while (await reader.ReadAsync())
                {
                    tableCount++;
                }

                await conn.CloseAsync();

                connection.LastSyncAt = DateTime.UtcNow;
                connection.LastSyncStatus = "Success";
                await _context.SaveChangesAsync();

                return new SyncResult
                {
                    Success = true,
                    RecordsProcessed = tableCount,
                    SyncTime = DateTime.UtcNow,
                    Message = $"Synced {tableCount} tables from MySQL database."
                };
            }
            catch (Exception ex)
            {
                connection.LastSyncStatus = "Failed";
                await _context.SaveChangesAsync();

                return new SyncResult
                {
                    Success = false,
                    Message = "Sync failed",
                    SyncTime = DateTime.UtcNow,
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<HealthStatus> GetHealthAsync(int connectionId)
        {
            var connection = await _context.DataSourceConnections.FindAsync(connectionId);
            if (connection == null)
            {
                return new HealthStatus
                {
                    IsHealthy = false,
                    Status = "Error",
                    LastChecked = DateTime.UtcNow,
                    Message = "Connection not found"
                };
            }

            var testResult = await TestConnectionAsync(connection);

            return new HealthStatus
            {
                IsHealthy = testResult.Success,
                Status = testResult.Success ? "Connected" : "Error",
                LastChecked = DateTime.UtcNow,
                Message = testResult.Message,
                Details = testResult.Metadata
            };
        }

        public string GetConfigurationSchema()
        {
            return @"{
                ""type"": ""object"",
                ""properties"": {
                    ""server"": { ""type"": ""string"", ""description"": ""MySQL server hostname or IP"" },
                    ""port"": { ""type"": ""integer"", ""default"": 3306 },
                    ""database"": { ""type"": ""string"", ""description"": ""Database name"" },
                    ""username"": { ""type"": ""string"" },
                    ""password"": { ""type"": ""string"", ""format"": ""password"" },
                    ""ssl"": { ""type"": ""boolean"", ""default"": false },
                    ""connectionTimeout"": { ""type"": ""integer"", ""default"": 30 }
                },
                ""required"": [""server"", ""database"", ""username"", ""password""]
            }";
        }

        public async Task<List<string>> GetTablesAsync(DataSourceConnection connection)
        {
            var tables = new List<string>();

            try
            {
                using var conn = new MySqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand("SHOW TABLES", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }

                await conn.CloseAsync();
            }
            catch (Exception)
            {
                // Return empty list on error
            }

            return tables;
        }

        public async Task<Dictionary<string, string>> GetTableSchemaAsync(DataSourceConnection connection, string tableName)
        {
            var schema = new Dictionary<string, string>();

            try
            {
                using var conn = new MySqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand($"DESCRIBE `{tableName}`", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var field = reader.GetString(0);
                    var type = reader.GetString(1);
                    var nullable = reader.GetString(2);
                    var key = reader.GetString(3);

                    var details = $"{type}";
                    if (nullable == "NO") details += " NOT NULL";
                    if (!string.IsNullOrEmpty(key)) details += $" {key}";

                    schema[field] = details;
                }

                await conn.CloseAsync();
            }
            catch (Exception ex)
            {
                schema["Error"] = ex.Message;
            }

            return schema;
        }

        public async Task<DataTable> ExecuteQueryAsync(DataSourceConnection connection, string query)
        {
            var dt = new DataTable();

            try
            {
                using var conn = new MySqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(query, conn);
                using var adapter = new MySqlDataAdapter(cmd);

                adapter.Fill(dt);

                await conn.CloseAsync();
            }
            catch (Exception ex)
            {
                dt.Columns.Clear();
                dt.Columns.Add("Error", typeof(string));
                dt.Rows.Add(ex.Message);
            }

            return dt;
        }
    }
}
