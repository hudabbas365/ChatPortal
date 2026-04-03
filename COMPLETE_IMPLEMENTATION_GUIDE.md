# 🚀 DATA SOURCE CONNECTOR AGENT SYSTEM - COMPLETE GUIDE

## 📋 CURRENT STATUS

### ✅ COMPLETED (Just Now)
1. **NuGet Packages Installed**:
   - `MySqlConnector 2.3.5` ✅
   - `Npgsql 8.0.2` ✅
   - `MongoDB.Driver 2.24.0` ✅
   - `AWSSDK.S3 3.7.307` ✅

2. **Base Infrastructure Created** (Previous Work):
   - `DataSourceConnection` entity ✅
   - `IDataSourceConnector` interface ✅
   - `DataSourceController` with JWT ✅
   - Provider registry (50+ services) ✅
   - UI modal for provider selection ✅

3. **New Entities Created**:
   - `QueryHistory.cs` ✅
   - `ChartDefinition.cs` ✅

### ⏳ IN PROGRESS
- MySQL connector update (started)

### ⏸️ PENDING (Next Steps)
Everything else from the requirements

---

## 🎯 STEP-BY-STEP COMPLETION GUIDE

This system requires approximately **40-60 hours of development**. Below is the recommended approach:

---

## PHASE 1: Complete Real Connector Implementations (6-8 hours)

### Step 1.1: Finish MySQL Connector
**File**: `Services/DataSourceConnectors/SQLConnectors/MySQLConnector.cs`

```csharp
// Complete SyncDataAsync method:
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

        // Query to count tables
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE()", conn);
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

// Add new method for schema discovery:
public async Task<List<string>> GetTablesAsync(DataSourceConnection connection)
{
    var tables = new List<string>();
    
    using var conn = new MySqlConnection(connection.ConnectionString);
    await conn.OpenAsync();
    
    using var cmd = new MySqlCommand("SHOW TABLES", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        tables.Add(reader.GetString(0));
    }
    
    return tables;
}

// Add method for table schema:
public async Task<Dictionary<string, string>> GetTableSchemaAsync(DataSourceConnection connection, string tableName)
{
    var schema = new Dictionary<string, string>();
    
    using var conn = new MySqlConnection(connection.ConnectionString);
    await conn.OpenAsync();
    
    using var cmd = new MySqlCommand($"DESCRIBE `{tableName}`", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    
    while (await reader.ReadAsync())
    {
        var columnName = reader.GetString(0);
        var columnType = reader.GetString(1);
        schema[columnName] = columnType;
    }
    
    return schema;
}

// Add method for executing queries:
public async Task<DataTable> ExecuteQueryAsync(DataSourceConnection connection, string query)
{
    var dataTable = new DataTable();
    
    using var conn = new MySqlConnection(connection.ConnectionString);
    await conn.OpenAsync();
    
    using var cmd = new MySqlCommand(query, conn);
    using var adapter = new MySqlDataAdapter(cmd);
    
    adapter.Fill(dataTable);
    
    return dataTable;
}
```

### Step 1.2: Update PostgreSQL Connector
**File**: `Services/DataSourceConnectors/SQLConnectors/PostgreSQLConnector.cs`

Follow same pattern as MySQL but use:
- `Npgsql.NpgsqlConnection`
- `SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'` for tables
- `SELECT column_name, data_type FROM information_schema.columns WHERE table_name = $1` for schema

### Step 1.3: Update MongoDB Connector
**File**: `Services/DataSourceConnectors/NoSQLConnectors/MongoDBConnector.cs`

```csharp
using MongoDB.Driver;
using MongoDB.Bson;

public async Task<List<string>> GetCollectionsAsync(DataSourceConnection connection)
{
    var client = new MongoClient(connection.ConnectionString);
    var database = client.GetDatabase(GetDatabaseName(connection.ConnectionString));
    
    var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();
    return collections;
}

public async Task<List<BsonDocument>> QueryCollectionAsync(DataSourceConnection connection, string collectionName, BsonDocument filter, int limit = 100)
{
    var client = new MongoClient(connection.ConnectionString);
    var database = client.GetDatabase(GetDatabaseName(connection.ConnectionString));
    var collection = database.GetCollection<BsonDocument>(collectionName);
    
    var documents = await collection.Find(filter).Limit(limit).ToListAsync();
    return documents;
}

private string GetDatabaseName(string connectionString)
{
    var url = new MongoUrl(connectionString);
    return url.DatabaseName ?? "test";
}
```

### Step 1.4: Update S3 Connector
**File**: `Services/DataSourceConnectors/CloudStorageConnectors/S3Connector.cs`

```csharp
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

public async Task<List<string>> ListBucketsAsync(DataSourceConnection connection)
{
    var credentials = new BasicAWSCredentials(connection.Username!, connection.ApiKey!);
    var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
    
    using var client = new AmazonS3Client(credentials, config);
    var response = await client.ListBucketsAsync();
    
    return response.Buckets.Select(b => b.BucketName).ToList();
}

public async Task<List<S3Object>> ListObjectsAsync(DataSourceConnection connection, string bucketName, int maxKeys = 1000)
{
    var credentials = new BasicAWSCredentials(connection.Username!, connection.ApiKey!);
    var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
    
    using var client = new AmazonS3Client(credentials, config);
    var request = new ListObjectsV2Request
    {
        BucketName = bucketName,
        MaxKeys = maxKeys
    };
    
    var response = await client.ListObjectsV2Async(request);
    return response.S3Objects;
}
```

---

## PHASE 2: Create Remaining Entities & Migration (2-3 hours)

### Step 2.1: Create Report Entity
**File**: `Models/Entities/Report.cs`

```csharp
public class Report
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    public int? OrganizationId { get; set; }
    
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public string Layout { get; set; } = "{}"; // JSON: grid positions
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsPublic { get; set; }
    
    [MaxLength(100)]
    public string? PublicToken { get; set; }
    
    public bool AllowEmbedding { get; set; }
    
    [MaxLength(100)]
    public string? EmbedToken { get; set; }
    
    public virtual ICollection<ReportChart> ReportCharts { get; set; } = new List<ReportChart>();
}
```

### Step 2.2: Create ReportChart Entity
**File**: `Models/Entities/ReportChart.cs`

```csharp
public class ReportChart
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ReportId { get; set; }
    
    [ForeignKey("ReportId")]
    public virtual Report Report { get; set; } = null!;
    
    [Required]
    public int ChartDefinitionId { get; set; }
    
    [ForeignKey("ChartDefinitionId")]
    public virtual ChartDefinition ChartDefinition { get; set; } = null!;
    
    public int Position { get; set; } // Order in report
    
    public string? CustomConfig { get; set; } // JSON override for chart config
}
```

### Step 2.3: Create Dashboard & DashboardChart Entities
Similar to Report/ReportChart but with `OrganizationId` instead of `UserId`

### Step 2.4: Create EmbedAccess Entity
**File**: `Models/Entities/EmbedAccess.cs`

```csharp
public class EmbedAccess
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty; // Report, Dashboard
    
    [Required]
    public int ResourceId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string EmbedUrl { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public int CreatedByUserId { get; set; }
    
    [ForeignKey("CreatedByUserId")]
    public virtual User CreatedBy { get; set; } = null!;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? RevokedAt { get; set; }
    
    public int? RevokedByUserId { get; set; }
    
    [ForeignKey("RevokedByUserId")]
    public virtual User? RevokedBy { get; set; }
    
    public int ViewCount { get; set; }
    
    public DateTime? LastAccessedAt { get; set; }
}
```

### Step 2.5: Update AppDbContext
**File**: `Data/AppDbContext.cs`

```csharp
public DbSet<QueryHistory> QueryHistories { get; set; }
public DbSet<ChartDefinition> ChartDefinitions { get; set; }
public DbSet<Report> Reports { get; set; }
public DbSet<ReportChart> ReportCharts { get; set; }
public DbSet<Dashboard> Dashboards { get; set; }
public DbSet<DashboardChart> DashboardCharts { get; set; }
public DbSet<EmbedAccess> EmbedAccesses { get; set; }
```

### Step 2.6: Create Migration
```bash
dotnet ef migrations add AddQueryHistoryAndCharts
dotnet ef database update
```

---

## PHASE 3: Build Dynamic REST API (4-6 hours)

### Step 3.1: Create DataSourceApiController
**File**: `Controllers/DataSourceApiController.cs`

```csharp
[Authorize]
[Route("api/datasource/{connectionId}")]
[ApiController]
public class DataSourceApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    
    public DataSourceApiController(AppDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }
    
    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    [HttpGet("tables")]
    public async Task<IActionResult> GetTables(int connectionId)
    {
        var connection = await _context.DataSourceConnections
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == GetUserId());
            
        if (connection == null)
            return NotFound();
            
        var provider = DataSourceProviderRegistry.GetAllProviders()
            .FirstOrDefault(p => p.Id == connection.Provider.ToLower());
            
        if (provider == null)
            return BadRequest("Provider not found");
            
        var connector = (IDataSourceConnector)ActivatorUtilities.CreateInstance(_serviceProvider, provider.ConnectorType);
        
        // Call GetTablesAsync method (needs to be added to IDataSourceConnector)
        var tables = await connector.GetTablesAsync(connection);
        
        return Ok(new { success = true, tables });
    }
    
    [HttpGet("schema/{tableName}")]
    public async Task<IActionResult> GetTableSchema(int connectionId, string tableName)
    {
        // Similar pattern - get connection, validate, get connector, call GetTableSchemaAsync
    }
    
    [HttpPost("query")]
    public async Task<IActionResult> ExecuteQuery(int connectionId, [FromBody] QueryRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var connection = await _context.DataSourceConnections
                .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == GetUserId());
                
            if (connection == null)
                return NotFound();
                
            var provider = DataSourceProviderRegistry.GetAllProviders()
                .FirstOrDefaultAsync(p => p.Id == connection.Provider.ToLower());
                
            var connector = (IDataSourceConnector)ActivatorUtilities.CreateInstance(_serviceProvider, provider.ConnectorType);
            
            var dataTable = await connector.ExecuteQueryAsync(connection, request.Query);
            
            stopwatch.Stop();
            
            // Save to QueryHistory
            var queryHistory = new QueryHistory
            {
                UserId = GetUserId(),
                DataSourceConnectionId = connectionId,
                Query = request.Query,
                QueryType = DetectQueryType(request.Query),
                ExecutedAt = DateTime.UtcNow,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                RowsAffected = dataTable.Rows.Count,
                Status = "Success",
                ResultSnapshot = SerializeDataTable(dataTable, 100) // First 100 rows
            };
            
            _context.QueryHistories.Add(queryHistory);
            await _context.SaveChangesAsync();
            
            return Ok(new
            {
                success = true,
                queryId = queryHistory.Id,
                executionTimeMs = queryHistory.ExecutionTimeMs,
                rowCount = dataTable.Rows.Count,
                results = SerializeDataTable(dataTable, 1000),
                suggestedCharts = GenerateChartSuggestions(dataTable)
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Save failed query
            var queryHistory = new QueryHistory
            {
                UserId = GetUserId(),
                DataSourceConnectionId = connectionId,
                Query = request.Query,
                QueryType = DetectQueryType(request.Query),
                ExecutedAt = DateTime.UtcNow,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                RowsAffected = 0,
                Status = "Failed",
                ErrorMessage = ex.Message
            };
            
            _context.QueryHistories.Add(queryHistory);
            await _context.SaveChangesAsync();
            
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
    
    private string DetectQueryType(string query)
    {
        query = query.Trim().ToUpper();
        if (query.StartsWith("SELECT")) return "SELECT";
        if (query.StartsWith("INSERT")) return "INSERT";
        if (query.StartsWith("UPDATE")) return "UPDATE";
        if (query.StartsWith("DELETE")) return "DELETE";
        return "OTHER";
    }
    
    private string SerializeDataTable(DataTable dt, int maxRows)
    {
        var data = new
        {
            columns = dt.Columns.Cast<DataColumn>().Select(c => new { name = c.ColumnName, type = c.DataType.Name }).ToArray(),
            rows = dt.AsEnumerable().Take(maxRows).Select(row => dt.Columns.Cast<DataColumn>().Select(col => row[col]).ToArray()).ToArray()
        };
        
        return JsonSerializer.Serialize(data);
    }
    
    private object GenerateChartSuggestions(DataTable dt)
    {
        // Simple heuristic for chart suggestions
        var suggestions = new List<object>();
        
        var numericColumns = dt.Columns.Cast<DataColumn>()
            .Where(c => c.DataType == typeof(int) || c.DataType == typeof(decimal) || c.DataType == typeof(double))
            .ToList();
            
        var stringColumns = dt.Columns.Cast<DataColumn>()
            .Where(c => c.DataType == typeof(string))
            .ToList();
            
        if (stringColumns.Any() && numericColumns.Any())
        {
            suggestions.Add(new
            {
                type = "bar",
                xAxis = stringColumns.First().ColumnName,
                yAxis = numericColumns.First().ColumnName,
                title = $"{numericColumns.First().ColumnName} by {stringColumns.First().ColumnName}"
            });
        }
        
        if (numericColumns.Count >= 2)
        {
            suggestions.Add(new
            {
                type = "scatter",
                xAxis = numericColumns[0].ColumnName,
                yAxis = numericColumns[1].ColumnName,
                title = $"{numericColumns[1].ColumnName} vs {numericColumns[0].ColumnName}"
            });
        }
        
        return suggestions;
    }
}

public class QueryRequest
{
    public string Query { get; set; } = string.Empty;
}
```

---

## PHASE 4: Implement AI Query Agent (6-8 hours)

### Step 4.1: Create OpenAI Service
**File**: `Services/OpenAI/IOpenAIService.cs`

```csharp
public interface IOpenAIService
{
    Task<string> GenerateCompletionAsync(string prompt, string systemMessage = "");
    Task<List<string>> GenerateSuggestedQueriesAsync(string tableName, Dictionary<string, string> schema);
}
```

### Step 4.2: Create Query Agent Service
**File**: `Services/QueryAgent/QueryAgentService.cs`

```csharp
public class QueryAgentService
{
    private readonly IOpenAIService _openAI;
    private readonly AppDbContext _context;
    
    public async Task<List<string>> GenerateSuggestedQueriesAsync(int connectionId, string tableName)
    {
        // Get table schema
        var connection = await _context.DataSourceConnections.FindAsync(connectionId);
        // Get connector, get schema
        
        // Use AI to generate queries
        var prompt = $"Given a table '{tableName}' with columns: {string.Join(", ", schema.Keys)}, generate 5 useful SQL queries";
        
        var response = await _openAI.GenerateCompletionAsync(prompt);
        
        // Parse response into query list
        return queries;
    }
    
    public async Task<QueryAnalysis> AnalyzeResultsAsync(int queryId)
    {
        var queryHistory = await _context.QueryHistories
            .Include(q => q.DataSourceConnection)
            .FirstOrDefaultAsync(q => q.Id == queryId);
            
        if (queryHistory == null || queryHistory.ResultSnapshot == null)
            return null;
            
        // Deserialize results
        var data = JsonSerializer.Deserialize<QueryResultData>(queryHistory.ResultSnapshot);
        
        // Use AI to analyze
        var prompt = $"Analyze these query results and provide insights:\n{queryHistory.ResultSnapshot}";
        
        var insights = await _openAI.GenerateCompletionAsync(prompt);
        
        return new QueryAnalysis
        {
            Insights = insights,
            RecommendedCharts = GenerateChartRecommendations(data)
        };
    }
}
```

---

## PHASE 5: Add Chart Visualization to Chat (4-6 hours)

### Step 5.1: Update Chat UI
**File**: `Views/Chat/Index.cshtml`

Add after message rendering:

```html
<div class="query-result-container" id="queryResult-{{queryId}}" style="display:none;">
    <div class="result-header">
        <span class="execution-time">Executed in {{timeMs}}ms</span>
        <button class="btn btn-sm btn-outline-primary" onclick="pinChart({{queryId}})">
            <i class="bi bi-pin"></i> Pin
        </button>
    </div>
    
    <div class="result-table">
        <table class="table table-sm">
            <thead id="resultTableHead-{{queryId}}"></thead>
            <tbody id="resultTableBody-{{queryId}}"></tbody>
        </table>
    </div>
    
    <div class="result-charts mt-3">
        <canvas id="chart-{{queryId}}" width="400" height="200"></canvas>
    </div>
    
    <div class="chart-suggestions">
        <button class="btn btn-sm btn-outline-secondary" onclick="renderChart({{queryId}}, 'bar')">
            <i class="bi bi-bar-chart"></i> Bar
        </button>
        <button class="btn btn-sm btn-outline-secondary" onclick="renderChart({{queryId}}, 'line')">
            <i class="bi bi-graph-up"></i> Line
        </button>
        <button class="btn btn-sm btn-outline-secondary" onclick="renderChart({{queryId}}, 'pie')">
            <i class="bi bi-pie-chart"></i> Pie
        </button>
    </div>
</div>
```

### Step 5.2: Add Chart Rendering JavaScript
**File**: `wwwroot/js/chart-handler.js`

```javascript
let activeCharts = {};

async function executeQuery(connectionId, query) {
    try {
        const response = await fetch(`/api/datasource/${connectionId}/query`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getJwtToken()}`
            },
            body: JSON.stringify({ query })
        });
        
        const result = await response.json();
        
        if (result.success) {
            displayQueryResults(result);
        } else {
            showToast(result.error, 'danger');
        }
    } catch (error) {
        showToast('Query execution failed: ' + error.message, 'danger');
    }
}

function displayQueryResults(result) {
    const container = document.getElementById(`queryResult-${result.queryId}`);
    
    // Parse results
    const data = JSON.parse(result.results);
    
    // Populate table
    const thead = document.getElementById(`resultTableHead-${result.queryId}`);
    const tbody = document.getElementById(`resultTableBody-${result.queryId}`);
    
    thead.innerHTML = '<tr>' + data.columns.map(col => `<th>${col.name}</th>`).join('') + '</tr>';
    tbody.innerHTML = data.rows.map(row => 
        '<tr>' + row.map(cell => `<td>${cell}</td>`).join('') + '</tr>'
    ).join('');
    
    // Auto-render first suggested chart
    if (result.suggestedCharts && result.suggestedCharts.length > 0) {
        const suggestion = result.suggestedCharts[0];
        renderChartFromSuggestion(result.queryId, data, suggestion);
    }
    
    container.style.display = 'block';
}

function renderChartFromSuggestion(queryId, data, suggestion) {
    const ctx = document.getElementById(`chart-${queryId}`).getContext('2d');
    
    // Destroy existing chart
    if (activeCharts[queryId]) {
        activeCharts[queryId].destroy();
    }
    
    // Prepare chart data
    const labels = data.rows.map(row => row[0]); // First column as labels
    const values = data.rows.map(row => row[1]); // Second column as values
    
    const chartConfig = {
        type: suggestion.type,
        data: {
            labels: labels,
            datasets: [{
                label: suggestion.yAxis,
                data: values,
                backgroundColor: 'rgba(102, 126, 234, 0.6)',
                borderColor: 'rgba(102, 126, 234, 1)',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            plugins: {
                title: {
                    display: true,
                    text: suggestion.title
                }
            }
        }
    };
    
    activeCharts[queryId] = new Chart(ctx, chartConfig);
}

async function pinChart(queryId) {
    const chartConfig = activeCharts[queryId].config;
    
    try {
        const response = await fetch('/Chart/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getJwtToken()}`
            },
            body: JSON.stringify({
                queryHistoryId: queryId,
                name: `Chart from Query ${queryId}`,
                chartType: chartConfig.type,
                dataConfig: JSON.stringify(chartConfig)
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast('Chart pinned successfully!', 'success');
        }
    } catch (error) {
        showToast('Failed to pin chart: ' + error.message, 'danger');
    }
}
```

---

## PHASE 6: Build Report/Dashboard System (8-10 hours)

### Step 6.1: Create ReportController
**File**: `Controllers/ReportController.cs`

```csharp
[Authorize]
public class ReportController : Controller
{
    private readonly AppDbContext _context;
    
    public IActionResult Create() => View();
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateReportViewModel model)
    {
        var report = new Report
        {
            UserId = GetUserId(),
            OrganizationId = await GetActiveOrganizationIdAsync(),
            Name = model.Name,
            Description = model.Description,
            Layout = "{}",
            PublicToken = GenerateToken(),
            EmbedToken = GenerateToken()
        };
        
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();
        
        return RedirectToAction("Edit", new { id = report.Id });
    }
    
    public async Task<IActionResult> Edit(int id)
    {
        var report = await _context.Reports
            .Include(r => r.ReportCharts)
                .ThenInclude(rc => rc.ChartDefinition)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == GetUserId());
            
        if (report == null)
            return NotFound();
            
        return View(report);
    }
    
    [HttpPost]
    public async Task<IActionResult> AddChart(int reportId, int chartId)
    {
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == GetUserId());
            
        if (report == null)
            return NotFound();
            
        var chart = await _context.ChartDefinitions
            .FirstOrDefaultAsync(c => c.Id == chartId && c.UserId == GetUserId());
            
        if (chart == null)
            return NotFound();
            
        var reportChart = new ReportChart
        {
            ReportId = reportId,
            ChartDefinitionId = chartId,
            Position = _context.ReportCharts.Count(rc => rc.ReportId == reportId) + 1
        };
        
        _context.ReportCharts.Add(reportChart);
        await _context.SaveChangesAsync();
        
        return Ok(new { success = true });
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Public(string token)
    {
        var report = await _context.Reports
            .Include(r => r.ReportCharts)
                .ThenInclude(rc => rc.ChartDefinition)
            .FirstOrDefaultAsync(r => r.PublicToken == token && r.IsPublic);
            
        if (report == null)
            return NotFound();
            
        return View(report);
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Embed(string token)
    {
        var report = await _context.Reports
            .Include(r => r.ReportCharts)
                .ThenInclude(rc => rc.ChartDefinition)
            .FirstOrDefaultAsync(r => r.EmbedToken == token && r.AllowEmbedding);
            
        if (report == null)
            return NotFound();
            
        // Log access
        var embedAccess = await _context.EmbedAccesses
            .FirstOrDefaultAsync(e => e.ResourceType == "Report" && e.ResourceId == report.Id);
            
        if (embedAccess != null)
        {
            embedAccess.ViewCount++;
            embedAccess.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
            
        return View(report);
    }
    
    [HttpPost]
    public async Task<IActionResult> GeneratePublicUrl(int id)
    {
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == GetUserId());
            
        if (report == null)
            return NotFound();
            
        report.IsPublic = true;
        
        if (string.IsNullOrEmpty(report.PublicToken))
            report.PublicToken = GenerateToken();
            
        await _context.SaveChangesAsync();
        
        var url = Url.Action("Public", "Report", new { token = report.PublicToken }, Request.Scheme);
        
        return Ok(new { success = true, url });
    }
    
    [HttpPost]
    public async Task<IActionResult> GenerateEmbedCode(int id)
    {
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == GetUserId());
            
        if (report == null)
            return NotFound();
            
        report.AllowEmbedding = true;
        
        if (string.IsNullOrEmpty(report.EmbedToken))
            report.EmbedToken = GenerateToken();
            
        await _context.SaveChangesAsync();
        
        // Create embed access record
        var embedAccess = new EmbedAccess
        {
            ResourceType = "Report",
            ResourceId = id,
            EmbedUrl = Url.Action("Embed", "Report", new { token = report.EmbedToken }, Request.Scheme)!,
            CreatedByUserId = GetUserId()
        };
        
        _context.EmbedAccesses.Add(embedAccess);
        await _context.SaveChangesAsync();
        
        var iframeCode = $"<iframe src=\"{embedAccess.EmbedUrl}\" width=\"100%\" height=\"600\" frameborder=\"0\"></iframe>";
        
        return Ok(new { success = true, iframeCode, embedUrl = embedAccess.EmbedUrl });
    }
    
    private string GenerateToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "").Replace("/", "").Replace("=", "")
            .Substring(0, 32);
    }
    
    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    private async Task<int?> GetActiveOrganizationIdAsync()
    {
        var orgIdString = HttpContext.Session.GetString("ActiveOrganizationId");
        if (string.IsNullOrEmpty(orgIdString)) return null;
        return int.Parse(orgIdString);
    }
}
```

### Step 6.2: Create Report Views
**File**: `Views/Report/Edit.cshtml`

```html
@model Report

<div class="container">
    <h2>Edit Report: @Model.Name</h2>
    
    <div class="row">
        <div class="col-md-8">
            <div id="reportCanvas" class="border p-3 bg-light" style="min-height: 600px;">
                <!-- Grid layout for charts -->
                <div class="grid-container">
                    @foreach (var rc in Model.ReportCharts.OrderBy(rc => rc.Position))
                    {
                        <div class="chart-item" data-chart-id="@rc.ChartDefinitionId">
                            <canvas id="chart-@rc.ChartDefinitionId"></canvas>
                            <button class="btn btn-sm btn-danger" onclick="removeChart(@rc.Id)">Remove</button>
                        </div>
                    }
                </div>
            </div>
        </div>
        
        <div class="col-md-4">
            <h5>Available Charts</h5>
            <div id="availableCharts">
                <!-- List of pinned charts -->
            </div>
            
            <hr>
            
            <h5>Share Report</h5>
            <button class="btn btn-primary" onclick="generatePublicUrl()">Generate Public URL</button>
            <button class="btn btn-secondary" onclick="generateEmbedCode()">Generate Embed Code</button>
            
            <div id="shareLinks" class="mt-3" style="display:none;">
                <div class="form-group">
                    <label>Public URL</label>
                    <input type="text" class="form-control" id="publicUrl" readonly>
                </div>
                <div class="form-group">
                    <label>Embed Code</label>
                    <textarea class="form-control" id="embedCode" rows="3" readonly></textarea>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
async function loadAvailableCharts() {
    const response = await fetch('/Chart/GetPinned');
    const result = await response.json();
    
    const container = document.getElementById('availableCharts');
    container.innerHTML = result.charts.map(chart => `
        <div class="chart-preview">
            <span>${chart.name}</span>
            <button class="btn btn-sm btn-primary" onclick="addChartToReport(${chart.id})">Add</button>
        </div>
    `).join('');
}

async function addChartToReport(chartId) {
    const response = await fetch('/Report/AddChart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reportId: @Model.Id, chartId })
    });
    
    if (response.ok) {
        location.reload();
    }
}

async function generatePublicUrl() {
    const response = await fetch('/Report/GeneratePublicUrl', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: @Model.Id })
    });
    
    const result = await response.json();
    
    if (result.success) {
        document.getElementById('publicUrl').value = result.url;
        document.getElementById('shareLinks').style.display = 'block';
    }
}

async function generateEmbedCode() {
    const response = await fetch('/Report/GenerateEmbedCode', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: @Model.Id })
    });
    
    const result = await response.json();
    
    if (result.success) {
        document.getElementById('embedCode').value = result.iframeCode;
        document.getElementById('shareLinks').style.display = 'block';
    }
}

// Initialize
loadAvailableCharts();
</script>
```

---

## PHASE 7: Admin Controls for Embed Management (3-4 hours)

### Step 7.1: Add Methods to OrganizationController
**File**: `Controllers/OrganizationController.cs`

```csharp
[HttpGet]
public async Task<IActionResult> GetActiveEmbeds()
{
    var userId = GetUserId();
    var orgId = await GetActiveOrganizationIdAsync();
    
    if (orgId == null)
        return BadRequest("No active organization");
        
    // Check if user is admin
    var member = await _context.OrganizationMembers
        .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId);
        
    if (member == null || (member.Role != "Owner" && member.Role != "Admin"))
        return Unauthorized();
        
    var embeds = await _context.EmbedAccesses
        .Include(e => e.CreatedBy)
        .Where(e => e.IsActive)
        .ToListAsync();
        
    return Json(new
    {
        success = true,
        embeds = embeds.Select(e => new
        {
            id = e.Id,
            resourceType = e.ResourceType,
            resourceId = e.ResourceId,
            embedUrl = e.EmbedUrl,
            createdAt = e.CreatedAt,
            createdBy = e.CreatedBy.FirstName + " " + e.CreatedBy.LastName,
            viewCount = e.ViewCount,
            lastAccessedAt = e.LastAccessedAt
        })
    });
}

[HttpPost]
public async Task<IActionResult> RevokeEmbed([FromForm] int embedId)
{
    var userId = GetUserId();
    var orgId = await GetActiveOrganizationIdAsync();
    
    if (orgId == null)
        return BadRequest("No active organization");
        
    // Check if user is admin
    var member = await _context.OrganizationMembers
        .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.UserId == userId);
        
    if (member == null || (member.Role != "Owner" && member.Role != "Admin"))
        return Unauthorized();
        
    var embed = await _context.EmbedAccesses.FindAsync(embedId);
    
    if (embed == null)
        return NotFound();
        
    embed.IsActive = false;
    embed.RevokedAt = DateTime.UtcNow;
    embed.RevokedByUserId = userId;
    
    await _context.SaveChangesAsync();
    
    return Json(new { success = true, message = "Embed access revoked" });
}
```

### Step 7.2: Add Embed Management Tab to Settings
**File**: `Views/Account/Settings.cshtml`

Add new tab:

```html
<li class="nav-item">
    <a class="nav-link" id="embed-tab" data-bs-toggle="tab" href="#embed" role="tab">
        <i class="bi bi-code-square"></i> Embed Management
    </a>
</li>

<!-- Tab content -->
<div class="tab-pane fade" id="embed" role="tabpanel">
    <div class="card">
        <div class="card-header">
            <h5>Active Embeds</h5>
        </div>
        <div class="card-body">
            <div class="alert alert-info">
                <i class="bi bi-info-circle"></i> Manage all active iframe embeds across your organization. You can revoke access at any time.
            </div>
            
            <div id="embedsList">
                <!-- Populated by JavaScript -->
            </div>
        </div>
    </div>
</div>

<script>
async function loadActiveEmbeds() {
    const response = await fetch('/Organization/GetActiveEmbeds');
    const result = await response.json();
    
    if (!result.success) {
        document.getElementById('embedsList').innerHTML = '<p class="text-muted">No active embeds</p>';
        return;
    }
    
    const html = result.embeds.map(embed => `
        <div class="embed-item border p-3 mb-2">
            <div class="row align-items-center">
                <div class="col-md-3">
                    <strong>${embed.resourceType}</strong>
                    <br>
                    <small class="text-muted">ID: ${embed.resourceId}</small>
                </div>
                <div class="col-md-3">
                    <small>Created by: ${embed.createdBy}</small>
                    <br>
                    <small class="text-muted">${new Date(embed.createdAt).toLocaleDateString()}</small>
                </div>
                <div class="col-md-3">
                    <span class="badge bg-primary">Views: ${embed.viewCount}</span>
                    ${embed.lastAccessedAt ? `<br><small>Last: ${new Date(embed.lastAccessedAt).toLocaleString()}</small>` : ''}
                </div>
                <div class="col-md-3 text-end">
                    <button class="btn btn-sm btn-danger" onclick="confirmRevokeEmbed(${embed.id})">
                        <i class="bi bi-x-circle"></i> Revoke
                    </button>
                </div>
            </div>
        </div>
    `).join('');
    
    document.getElementById('embedsList').innerHTML = html;
}

function confirmRevokeEmbed(embedId) {
    showConfirmModal(
        'Revoke Embed Access',
        'Are you sure you want to revoke this embed? All existing embeds will stop working immediately.',
        async () => {
            const formData = new FormData();
            formData.append('embedId', embedId);
            formData.append('__RequestVerificationToken', getAntiForgeryToken());
            
            const response = await fetch('/Organization/RevokeEmbed', {
                method: 'POST',
                body: formData
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast('Embed access revoked successfully', 'success');
                loadActiveEmbeds();
            } else {
                showToast('Failed to revoke embed: ' + result.error, 'danger');
            }
        }
    );
}

// Load on tab activation
document.getElementById('embed-tab').addEventListener('shown.bs.tab', loadActiveEmbeds);
</script>
```

---

## FINAL STEPS

### 1. Update IDataSourceConnector Interface
Add new methods to interface:

```csharp
Task<List<string>> GetTablesAsync(DataSourceConnection connection);
Task<Dictionary<string, string>> GetTableSchemaAsync(DataSourceConnection connection, string tableName);
Task<DataTable> ExecuteQueryAsync(DataSourceConnection connection, string query);
```

### 2. Register Services in Program.cs
```csharp
builder.Services.AddScoped<IQueryAgentService, QueryAgentService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
```

### 3. Configure OpenAI
Add to `appsettings.json`:
```json
"OpenAI": {
  "ApiKey": "your-openai-api-key",
  "Model": "gpt-4",
  "MaxTokens": 2000
}
```

### 4. Run Migration
```bash
dotnet ef migrations add AddQueryHistoryChartsReportsDashboards
dotnet ef database update
```

### 5. Build and Test
```bash
dotnet build
dotnet run
```

---

## TESTING CHECKLIST

- [ ] Connect to MySQL database
- [ ] List tables via API
- [ ] Execute SELECT query
- [ ] View query results in chat
- [ ] See auto-generated chart
- [ ] Pin chart
- [ ] Create report
- [ ] Add charts to report
- [ ] Generate public URL
- [ ] Generate embed code
- [ ] Test public access (logged out)
- [ ] Test embed in iframe
- [ ] Admin revokes embed
- [ ] Verify embed stops working

---

## ESTIMATED TIMELINE

- Phase 1 (Connectors): 6-8 hours
- Phase 2 (Entities): 2-3 hours
- Phase 3 (API): 4-6 hours
- Phase 4 (AI Agent): 6-8 hours
- Phase 5 (Charts): 4-6 hours
- Phase 6 (Reports): 8-10 hours
- Phase 7 (Admin): 3-4 hours

**Total: 33-45 hours of development**

This is a full-scale feature requiring significant development time. Recommend implementing in sprints:
- Sprint 1: Phases 1-3 (MVP: Connect, query, view results)
- Sprint 2: Phases 4-5 (AI + Charts)
- Sprint 3: Phases 6-7 (Reports + Admin)
