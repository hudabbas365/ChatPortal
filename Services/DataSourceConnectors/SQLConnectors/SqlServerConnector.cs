using ChatPortal.Models.Entities;
using ChatPortal.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ChatPortal.Services.DataSourceConnectors.SQLConnectors
{
    public class SqlServerConnector : IDataSourceConnector
    {
        private readonly AppDbContext _context;

        public SqlServerConnector(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ConnectionResult> TestConnectionAsync(DataSourceConnection connection)
        {
            try
            {
                using var conn = new SqlConnection(connection.ConnectionString);
                await conn.OpenAsync();
                
                if (conn.State == ConnectionState.Open)
                {
                    var version = conn.ServerVersion;
                    await conn.CloseAsync();
                    
                    return new ConnectionResult
                    {
                        Success = true,
                        Message = "Successfully connected to SQL Server database",
                        Metadata = new Dictionary<string, object>
                        {
                            { "ServerVersion", version },
                            { "Database", conn.Database }
                        }
                    };
                }

                return new ConnectionResult
                {
                    Success = false,
                    Message = "Failed to open SQL Server connection"
                };
            }
            catch (Exception ex)
            {
                return new ConnectionResult
                {
                    Success = false,
                    Message = "SQL Server connection test failed",
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
                using var conn = new SqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES", conn);
                var tableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                connection.LastSyncAt = DateTime.UtcNow;
                connection.LastSyncStatus = "Success";
                await _context.SaveChangesAsync();

                return new SyncResult
                {
                    Success = true,
                    RecordsProcessed = tableCount,
                    SyncTime = DateTime.UtcNow,
                    Message = $"Successfully synced. Found {tableCount} tables."
                };
            }
            catch (Exception ex)
            {
                connection.LastSyncStatus = "Failed";
                await _context.SaveChangesAsync();

                return new SyncResult
                {
                    Success = false,
                    RecordsProcessed = 0,
                    SyncTime = DateTime.UtcNow,
                    Message = "Sync failed",
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
                    ""server"": { ""type"": ""string"", ""description"": ""SQL Server hostname or IP"" },
                    ""database"": { ""type"": ""string"", ""description"": ""Database name"" },
                    ""username"": { ""type"": ""string"" },
                    ""password"": { ""type"": ""string"", ""format"": ""password"" },
                    ""integratedSecurity"": { ""type"": ""boolean"", ""default"": false },
                    ""encrypt"": { ""type"": ""boolean"", ""default"": true },
                    ""trustServerCertificate"": { ""type"": ""boolean"", ""default"": false }
                },
                ""required"": [""server"", ""database""]
            }";
        }

        public async Task<List<string>> GetTablesAsync(DataSourceConnection connection)
        {
            var tables = new List<string>();

            try
            {
                using var conn = new SqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", conn);
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
                using var conn = new SqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                var query = @"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT
                             FROM INFORMATION_SCHEMA.COLUMNS 
                             WHERE TABLE_NAME = @tableName";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tableName", tableName);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var columnName = reader.GetString(0);
                    var dataType = reader.GetString(1);
                    var isNullable = reader.GetString(2);
                    var maxLength = reader.IsDBNull(3) ? "" : $"({reader.GetInt32(3)})";

                    var details = $"{dataType}{maxLength}";
                    if (isNullable == "NO") details += " NOT NULL";

                    schema[columnName] = details;
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
                using var conn = new SqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(query, conn);
                using var adapter = new SqlDataAdapter(cmd);

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
