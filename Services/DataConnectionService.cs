using System.Data;
using System.Text.Json;
using ChatPortal.Data;
using ChatPortal.Models.Entities;
using ClosedXML.Excel;
using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Npgsql;
using MySqlConnector;
using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;
using Elastic.Clients.Elasticsearch;
using Oracle.ManagedDataAccess.Client;

namespace ChatPortal.Services;

public class DataConnectionService : IDataConnectionService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DataConnectionService> _logger;

    private static readonly string[] AllowedExtensions = { ".xlsx", ".xls", ".csv" };
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    public DataConnectionService(AppDbContext db, IWebHostEnvironment env, ILogger<DataConnectionService> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task<List<UserDataSource>> GetUserDataSourcesAsync(int userId)
    {
        return await _db.UserDataSources
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserDataSource?> GetDataSourceAsync(int id, int userId)
    {
        return await _db.UserDataSources
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
    }

    public async Task<UserDataSource> CreateFileDataSourceAsync(int userId, string name, string sourceType, IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not supported. Allowed: .xlsx, .xls, .csv");

        if (file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File size exceeds the 50 MB limit.");

        var uploadsDir = Path.Combine(_env.ContentRootPath, "PrivateUploads", "datasources", userId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var safeFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, safeFileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var relativeFilePath = Path.Combine("PrivateUploads", "datasources", userId.ToString(), safeFileName);
        var schemaSnapshot = ext == ".csv"
            ? GetCsvSchema(filePath)
            : GetExcelSchema(filePath);

        var ds = new UserDataSource
        {
            UserId = userId,
            Name = name,
            SourceType = sourceType,
            FilePath = relativeFilePath,
            SchemaSnapshot = JsonSerializer.Serialize(schemaSnapshot),
            Status = "Active"
        };

        _db.UserDataSources.Add(ds);
        await _db.SaveChangesAsync();
        return ds;
    }

    public async Task<UserDataSource> CreateDatabaseDataSourceAsync(int userId, string name, string sourceType, string connectionString)
    {
        var tables = await GetAvailableTablesAsync(sourceType, connectionString);

        var ds = new UserDataSource
        {
            UserId = userId,
            Name = name,
            SourceType = sourceType,
            ConnectionDetails = connectionString,
            SchemaSnapshot = JsonSerializer.Serialize(tables),
            Status = "Active"
        };

        _db.UserDataSources.Add(ds);
        await _db.SaveChangesAsync();
        return ds;
    }

    public async Task<bool> ValidateConnectionAsync(string sourceType, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Length > 2000)
            return false;

        try
        {
            switch (sourceType)
            {
                case "SqlServer":
                    var _ = new SqlConnectionStringBuilder(connectionString);
                    await using (var conn = new SqlConnection(connectionString))
                        await conn.OpenAsync();
                    return true;

                case "PostgreSQL":
                    await using (var pgConn = new NpgsqlConnection(connectionString))
                        await pgConn.OpenAsync();
                    return true;

                case "MySQL":
                    await using (var myConn = new MySqlConnection(connectionString))
                        await myConn.OpenAsync();
                    return true;

                case "MongoDB":
                    var mongoClient = new MongoClient(connectionString);
                    await mongoClient.ListDatabaseNamesAsync();
                    return true;

                case "Oracle":
                    await using (var orConn = new OracleConnection(connectionString))
                        await orConn.OpenAsync();
                    return true;

                case "Elasticsearch":
                    var esSettings = new ElasticsearchClientSettings(new Uri(connectionString));
                    var esClient = new ElasticsearchClient(esSettings);
                    var pingResult = await esClient.PingAsync();
                    return pingResult.IsSuccess();

                case "Redis":
                    var redisConn = await ConnectionMultiplexer.ConnectAsync(connectionString);
                    return redisConn.IsConnected;

                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection validation failed for source type {SourceType}", sourceType);
            return false;
        }
    }

    public async Task<List<string>> GetAvailableTablesAsync(string sourceType, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty.");

        var tables = new List<string>();

        switch (sourceType)
        {
            case "SqlServer":
                ValidateSqlConnectionString(connectionString);
                await using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT TABLE_SCHEMA + '.' + TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE IN ('BASE TABLE','VIEW') ORDER BY TABLE_NAME";
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        tables.Add(reader.GetString(0));
                }
                break;

            case "PostgreSQL":
                await using (var pgConn = new NpgsqlConnection(connectionString))
                {
                    await pgConn.OpenAsync();
                    await using var pgCmd = new NpgsqlCommand(
                        "SELECT table_schema || '.' || table_name FROM information_schema.tables WHERE table_schema NOT IN ('pg_catalog','information_schema') ORDER BY table_name",
                        pgConn);
                    await using var pgReader = await pgCmd.ExecuteReaderAsync();
                    while (await pgReader.ReadAsync())
                        tables.Add(pgReader.GetString(0));
                }
                break;

            case "MySQL":
                await using (var myConn = new MySqlConnection(connectionString))
                {
                    await myConn.OpenAsync();
                    await using var myCmd = new MySqlCommand(
                        "SELECT CONCAT(table_schema, '.', table_name) FROM information_schema.tables WHERE table_schema NOT IN ('information_schema','performance_schema','mysql','sys') ORDER BY table_name",
                        myConn);
                    await using var myReader = await myCmd.ExecuteReaderAsync();
                    while (await myReader.ReadAsync())
                        tables.Add(myReader.GetString(0));
                }
                break;

            case "MongoDB":
                var mongoClient = new MongoClient(connectionString);
                var mongoUrl = MongoUrl.Create(connectionString);
                var dbName = mongoUrl.DatabaseName ?? "admin";
                var mongoDb = mongoClient.GetDatabase(dbName);
                var collections = await mongoDb.ListCollectionNamesAsync();
                tables.AddRange(await collections.ToListAsync());
                break;

            case "Oracle":
                await using (var orConn = new OracleConnection(connectionString))
                {
                    await orConn.OpenAsync();
                    await using var orCmd = orConn.CreateCommand();
                    orCmd.CommandText = "SELECT OWNER || '.' || TABLE_NAME FROM ALL_TABLES ORDER BY TABLE_NAME";
                    await using var orReader = await orCmd.ExecuteReaderAsync();
                    while (await orReader.ReadAsync())
                        tables.Add(orReader.GetString(0));
                }
                break;

            case "Elasticsearch":
                var esSettings = new ElasticsearchClientSettings(new Uri(connectionString));
                var esClient = new ElasticsearchClient(esSettings);
                var indicesResult = await esClient.Indices.GetAsync(Indices.All);
                if (indicesResult.IsSuccess() && indicesResult.Indices != null)
                    tables.AddRange(indicesResult.Indices.Keys.Select(k => k.ToString()).Where(i => !string.IsNullOrEmpty(i)));
                break;

            case "Redis":
                var redisConn = await ConnectionMultiplexer.ConnectAsync(connectionString);
                var server = redisConn.GetServer(redisConn.GetEndPoints().First());
                tables.AddRange(server.Keys().Select(k => k.ToString()).Take(500));
                break;

            default:
                throw new NotSupportedException($"Source type '{sourceType}' is not supported for table discovery.");
        }

        return tables;
    }

    public async Task<Dictionary<string, List<string>>> GetSchemaAsync(int dataSourceId, int userId)
    {
        var ds = await _db.UserDataSources.FirstOrDefaultAsync(d => d.Id == dataSourceId && d.UserId == userId)
            ?? throw new KeyNotFoundException("Data source not found.");

        if (!string.IsNullOrEmpty(ds.SchemaSnapshot))
        {
            var existing = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(ds.SchemaSnapshot);
            if (existing != null) return existing;
        }

        var schema = new Dictionary<string, List<string>>();

        if (ds.SourceType is "Excel" or "CSV")
        {
            var fullPath = Path.Combine(_env.ContentRootPath, ds.FilePath!);
            return ds.SourceType == "CSV" ? GetCsvSchema(fullPath) : GetExcelSchema(fullPath);
        }

        var tables = await GetAvailableTablesAsync(ds.SourceType, ds.ConnectionDetails!);
        foreach (var t in tables)
            schema[t] = new List<string>();

        return schema;
    }

    public async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(int dataSourceId, int userId, string query)
    {
        return await QueryDataSourceAsync(dataSourceId, userId, query);
    }

    private static void ValidateSqlConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty.");

        if (connectionString.Length > 2000)
            throw new ArgumentException("Connection string is too long.");

        var _ = new SqlConnectionStringBuilder(connectionString);
    }

    public async Task<UserDataSource> UpdateSelectedTablesAsync(int id, int userId, List<string> selectedTables)
    {
        var ds = await _db.UserDataSources.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException("Data source not found.");

        ds.SelectedTables = JsonSerializer.Serialize(selectedTables);
        ds.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ds;
    }

    public async Task DeleteDataSourceAsync(int id, int userId)
    {
        var ds = await _db.UserDataSources.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException("Data source not found.");

        if (!string.IsNullOrEmpty(ds.FilePath))
        {
            var fullPath = Path.Combine(_env.ContentRootPath, ds.FilePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        _db.UserDataSources.Remove(ds);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Dictionary<string, object?>>> QueryDataSourceAsync(int dataSourceId, int userId, string query)
    {
        var ds = await _db.UserDataSources.FirstOrDefaultAsync(d => d.Id == dataSourceId && d.UserId == userId)
            ?? throw new KeyNotFoundException("Data source not found or access denied.");

        return ds.SourceType switch
        {
            "SqlServer" => await QuerySqlServerAsync(ds.ConnectionDetails!, query),
            "PostgreSQL" => await QueryPostgreSQLAsync(ds.ConnectionDetails!, query),
            "MySQL" => await QueryMySQLAsync(ds.ConnectionDetails!, query),
            "MongoDB" => await QueryMongoDBAsync(ds.ConnectionDetails!, query),
            "Oracle" => await QueryOracleAsync(ds.ConnectionDetails!, query),
            "Elasticsearch" => await QueryElasticsearchAsync(ds.ConnectionDetails!, query),
            "Excel" or "CSV" => QueryFileDataSource(ds, query),
            _ => throw new NotSupportedException($"Querying data source of type '{ds.SourceType}' is not supported.")
        };
    }

    // ── Schema helpers ──────────────────────────────────────────────────────

    private static Dictionary<string, List<string>> GetExcelSchema(string filePath)
    {
        var schema = new Dictionary<string, List<string>>();
        using var workbook = new XLWorkbook(filePath);
        foreach (var ws in workbook.Worksheets)
        {
            var headers = ws.Row(1).CellsUsed()
                            .Select(c => c.GetString())
                            .Where(h => !string.IsNullOrWhiteSpace(h))
                            .ToList();
            schema[ws.Name] = headers;
        }
        return schema;
    }

    private static Dictionary<string, List<string>> GetCsvSchema(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? new List<string>();
        return new Dictionary<string, List<string>> { ["Sheet1"] = headers };
    }

    // ── Query executors ─────────────────────────────────────────────────────

    private static async Task<List<Dictionary<string, object?>>> QuerySqlServerAsync(string connectionString, string sql)
    {
        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only SELECT queries are permitted for SQL Server data sources.");

        var results = new List<Dictionary<string, object?>>();
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 30;
        await using var reader = await cmd.ExecuteReaderAsync();
        int rowCount = 0;
        while (await reader.ReadAsync() && rowCount < 500)
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            results.Add(row);
            rowCount++;
        }
        return results;
    }

    private static async Task<List<Dictionary<string, object?>>> QueryPostgreSQLAsync(string connectionString, string sql)
    {
        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only SELECT queries are permitted for PostgreSQL data sources.");

        var results = new List<Dictionary<string, object?>>();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.CommandTimeout = 30;
        await using var reader = await cmd.ExecuteReaderAsync();
        int rowCount = 0;
        while (await reader.ReadAsync() && rowCount < 500)
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            results.Add(row);
            rowCount++;
        }
        return results;
    }

    private static async Task<List<Dictionary<string, object?>>> QueryMySQLAsync(string connectionString, string sql)
    {
        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only SELECT queries are permitted for MySQL data sources.");

        var results = new List<Dictionary<string, object?>>();
        await using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.CommandTimeout = 30;
        await using var reader = await cmd.ExecuteReaderAsync();
        int rowCount = 0;
        while (await reader.ReadAsync() && rowCount < 500)
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            results.Add(row);
            rowCount++;
        }
        return results;
    }

    private static async Task<List<Dictionary<string, object?>>> QueryMongoDBAsync(string connectionString, string collectionName)
    {
        var mongoUrl = MongoUrl.Create(connectionString);
        var dbName = mongoUrl.DatabaseName ?? "admin";
        var client = new MongoClient(connectionString);
        var db = client.GetDatabase(dbName);
        var collection = db.GetCollection<BsonDocument>(collectionName);
        var docs = await collection.Find(new BsonDocument()).Limit(500).ToListAsync();

        return docs.Select(doc =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var element in doc)
            {
                if (element.Name == "_id") continue;
                dict[element.Name] = element.Value.ToString();
            }
            return dict;
        }).ToList();
    }

    private static async Task<List<Dictionary<string, object?>>> QueryOracleAsync(string connectionString, string sql)
    {
        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only SELECT queries are permitted for Oracle data sources.");

        var results = new List<Dictionary<string, object?>>();
        await using var conn = new OracleConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 30;
        await using var reader = await cmd.ExecuteReaderAsync();
        int rowCount = 0;
        while (await reader.ReadAsync() && rowCount < 500)
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            results.Add(row);
            rowCount++;
        }
        return results;
    }

    private static async Task<List<Dictionary<string, object?>>> QueryElasticsearchAsync(string connectionString, string indexName)
    {
        var settings = new ElasticsearchClientSettings(new Uri(connectionString));
        var client = new ElasticsearchClient(settings);
        var searchResult = await client.SearchAsync<System.Text.Json.JsonElement>(s => s.Index(indexName).Size(500));

        if (!searchResult.IsSuccess())
            return new List<Dictionary<string, object?>>();

        return searchResult.Documents.Select(doc =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in doc.EnumerateObject())
                dict[prop.Name] = prop.Value.ToString();
            return dict;
        }).ToList();
    }

    private List<Dictionary<string, object?>> QueryFileDataSource(UserDataSource ds, string sheetOrQuery)
    {
        var results = new List<Dictionary<string, object?>>();
        var fullPath = Path.Combine(_env.ContentRootPath, ds.FilePath!);

        if (ds.SourceType == "CSV")
        {
            using var reader = new StreamReader(fullPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<dynamic>();
            foreach (var r in records.Take(200))
            {
                var dict = (IDictionary<string, object>)r;
                results.Add(dict.ToDictionary(k => k.Key, v => (object?)v.Value));
            }
        }
        else
        {
            using var workbook = new XLWorkbook(fullPath);
            var ws = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("No worksheets found.");
            var headers = ws.Row(1).CellsUsed().Select(c => c.GetString()).ToList();
            int rowCount = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 1, 201);
            for (int row = 2; row <= rowCount; row++)
            {
                var dict = new Dictionary<string, object?>();
                for (int col = 1; col <= headers.Count; col++)
                    dict[headers[col - 1]] = ws.Cell(row, col).GetString();
                results.Add(dict);
            }
        }

        return results;
    }
}
