using ChatPortal.Models.Entities;
using ChatPortal.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace ChatPortal.Services.DataSourceConnectors.SQLConnectors
{
    public class PostgreSQLConnector : IDataSourceConnector
    {
        private readonly AppDbContext _context;

        public PostgreSQLConnector(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ConnectionResult> TestConnectionAsync(DataSourceConnection connection)
        {
            try
            {
                using var conn = new NpgsqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                if (conn.State == ConnectionState.Open)
                {
                    var version = conn.ServerVersion;
                    var database = conn.Database;
                    await conn.CloseAsync();

                    return new ConnectionResult
                    {
                        Success = true,
                        Message = "Successfully connected to PostgreSQL database",
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
                    Message = "Failed to open PostgreSQL connection"
                };
            }
            catch (Exception ex)
            {
                return new ConnectionResult
                {
                    Success = false,
                    Message = "PostgreSQL connection test failed",
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
                using var conn = new NpgsqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'", conn);
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
                    ""host"": { ""type"": ""string"", ""description"": ""PostgreSQL server hostname or IP"" },
                    ""port"": { ""type"": ""integer"", ""default"": 5432 },
                    ""database"": { ""type"": ""string"", ""description"": ""Database name"" },
                    ""username"": { ""type"": ""string"" },
                    ""password"": { ""type"": ""string"", ""format"": ""password"" },
                    ""ssl"": { ""type"": ""boolean"", ""default"": false },
                    ""connectionTimeout"": { ""type"": ""integer"", ""default"": 30 }
                },
                ""required"": [""host"", ""database"", ""username"", ""password""]
            }";
        }

        public async Task<List<string>> GetTablesAsync(DataSourceConnection connection)
        {
            var tables = new List<string>();

            try
            {
                using var conn = new NpgsqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name", conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
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
                using var conn = new NpgsqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand($"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = @tableName AND table_schema = 'public' ORDER BY ordinal_position", conn);
                cmd.Parameters.AddWithValue("@tableName", tableName);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var columnName = reader.GetString(0);
                    var dataType = reader.GetString(1);
                    schema[columnName] = dataType;
                }
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
                using var conn = new NpgsqlConnection(connection.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.CommandTimeout = 30;

                using var adapter = new NpgsqlDataAdapter(cmd);
                adapter.Fill(dt);
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
