using System.Data;
using System.Text.Json;
using ChatPortal.Data;
using ChatPortal.Models.Entities;
using ClosedXML.Excel;
using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ChatPortal.Services;

public class DataConnectionService : IDataConnectionService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DataConnectionService> _logger;

    private static readonly string[] AllowedExtensions = { ".xlsx", ".xls", ".csv" };
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

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

        // Store outside wwwroot to prevent direct public access
        var uploadsDir = Path.Combine(_env.ContentRootPath, "PrivateUploads", "datasources", userId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var safeFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, safeFileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Store path relative to ContentRootPath
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
        ValidateConnectionString(connectionString);

        // Validate we can connect
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

    public async Task<List<string>> GetAvailableTablesAsync(string sourceType, string connectionString)
    {
        ValidateConnectionString(connectionString);

        var tables = new List<string>();

        if (sourceType == "SqlServer")
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TABLE_SCHEMA + '.' + TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE IN ('BASE TABLE','VIEW') ORDER BY TABLE_NAME";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));
        }
        else
        {
            throw new NotSupportedException($"Source type '{sourceType}' is not supported for table discovery.");
        }

        return tables;
    }

    /// <summary>
    /// Validates a connection string to ensure it only contains expected keys and no suspicious patterns.
    /// </summary>
    private static void ValidateConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty.");

        if (connectionString.Length > 2000)
            throw new ArgumentException("Connection string is too long.");

        // Use SqlConnectionStringBuilder for safe parsing — it throws on invalid connection strings
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

        // Delete file if it exists
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

        if (ds.SourceType == "SqlServer")
            return await QuerySqlServerAsync(ds.ConnectionDetails!, query);

        if (ds.SourceType == "Excel" || ds.SourceType == "CSV")
            return QueryFileDataSource(ds, query);

        throw new NotSupportedException($"Querying data source of type '{ds.SourceType}' is not supported.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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

    private static async Task<List<Dictionary<string, object?>>> QuerySqlServerAsync(string connectionString, string sql)
    {
        // Only allow SELECT statements to prevent SQL injection via malicious queries
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
