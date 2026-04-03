# DATA SOURCE CONNECTOR AGENT SYSTEM - IMPLEMENTATION PLAN

## ✅ COMPLETED
1. **NuGet Packages Installed**:
   - MySqlConnector 2.3.5
   - Npgsql 8.0.2
   - MongoDB.Driver 2.24.0
   - AWSSDK.S3 3.7.307

2. **Base Infrastructure Created**:
   - DataSourceConnection entity
   - IDataSourceConnector interface
   - DataSourceController with JWT security
   - Provider registry with 50+ services
   - UI with provider selection modal

## 📋 IMPLEMENTATION ROADMAP

### PHASE 1: Complete Connector Implementations (High Priority)
**Status**: In Progress
**Files to Update**:
- `Services/DataSourceConnectors/SQLConnectors/MySQLConnector.cs` ✅ Started
- `Services/DataSourceConnectors/SQLConnectors/PostgreSQLConnector.cs`
- `Services/DataSourceConnectors/NoSQLConnectors/MongoDBConnector.cs`
- `Services/DataSourceConnectors/CloudStorageConnectors/S3Connector.cs`

**Tasks**:
1. Remove all placeholder comments
2. Implement real connection logic using installed packages
3. Implement schema discovery (list tables/collections)
4. Implement data retrieval methods

### PHASE 2: New Entities for Query History & Charts
**Status**: Pending
**New Entities Needed**:

```csharp
// Models/Entities/QueryHistory.cs
public class QueryHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? OrganizationId { get; set; }
    public int DataSourceConnectionId { get; set; }
    public string Query { get; set; }
    public string QueryType { get; set; } // SELECT, INSERT, UPDATE, etc.
    public DateTime ExecutedAt { get; set; }
    public int ExecutionTimeMs { get; set; }
    public int RowsAffected { get; set; }
    public string Status { get; set; } // Success, Failed
    public string? ErrorMessage { get; set; }
    public string? ResultSnapshot { get; set; } // JSON
    public virtual User User { get; set; }
    public virtual DataSourceConnection DataSourceConnection { get; set; }
}

// Models/Entities/ChartDefinition.cs
public class ChartDefinition
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? OrganizationId { get; set; }
    public int? QueryHistoryId { get; set; }
    public string Name { get; set; }
    public string ChartType { get; set; } // bar, line, pie, scatter, etc.
    public string DataConfig { get; set; } // JSON: labels, datasets, options
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsPinned { get; set; }
    public virtual User User { get; set; }
    public virtual QueryHistory? QueryHistory { get; set; }
}

// Models/Entities/Report.cs
public class Report
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? OrganizationId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Layout { get; set; } // JSON: grid positions of charts
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsPublic { get; set; }
    public string? PublicToken { get; set; }
    public bool AllowEmbedding { get; set; }
    public string? EmbedToken { get; set; }
    public virtual User User { get; set; }
    public virtual ICollection<ReportChart> ReportCharts { get; set; }
}

// Models/Entities/ReportChart.cs
public class ReportChart
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int ChartDefinitionId { get; set; }
    public int Position { get; set; }
    public string? CustomConfig { get; set; } // JSON override
    public virtual Report Report { get; set; }
    public virtual ChartDefinition ChartDefinition { get; set; }
}

// Models/Entities/Dashboard.cs
public class Dashboard
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Layout { get; set; } // JSON
    public DateTime CreatedAt { get; set; }
    public bool IsPublic { get; set; }
    public string? PublicToken { get; set; }
    public bool AllowEmbedding { get; set; }
    public string? EmbedToken { get; set; }
    public virtual Organization Organization { get; set; }
    public virtual ICollection<DashboardChart> DashboardCharts { get; set; }
}

// Models/Entities/DashboardChart.cs
public class DashboardChart
{
    public int Id { get; set; }
    public int DashboardId { get; set; }
    public int ChartDefinitionId { get; set; }
    public int Position { get; set; }
    public virtual Dashboard Dashboard { get; set; }
    public virtual ChartDefinition ChartDefinition { get; set; }
}

// Models/Entities/EmbedAccess.cs
public class EmbedAccess
{
    public int Id { get; set; }
    public string ResourceType { get; set; } // Report, Dashboard
    public int ResourceId { get; set; }
    public string EmbedUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int? RevokedByUserId { get; set; }
}
```

### PHASE 3: Dynamic REST API Generation
**Status**: Pending
**New Controller**:

```csharp
// Controllers/DataSourceApiController.cs
[Authorize]
[Route("api/datasource/{connectionId}")]
public class DataSourceApiController : Controller
{
    // GET api/datasource/{connectionId}/tables
    // Returns list of tables/collections
    
    // GET api/datasource/{connectionId}/schema/{tableName}
    // Returns table schema (columns, types)
    
    // POST api/datasource/{connectionId}/query
    // Executes query, logs to QueryHistory
    
    // GET api/datasource/{connectionId}/query/{queryId}/results
    // Returns cached query results
}
```

### PHASE 4: AI Agent Query Generation
**Status**: Pending
**New Service**:

```csharp
// Services/QueryAgent/IQueryAgentService.cs
public interface IQueryAgentService
{
    Task<List<string>> GenerateSuggestedQueriesAsync(int connectionId, string tableName);
    Task<QueryAnalysisResult> AnalyzeQueryResultsAsync(string query, DataTable results);
    Task<ChartRecommendation> RecommendChartsAsync(DataTable results);
}

// Services/QueryAgent/QueryAgentService.cs
public class QueryAgentService : IQueryAgentService
{
    private readonly IOpenAIService _openAI;
    private readonly AppDbContext _context;
    
    public async Task<List<string>> GenerateSuggestedQueriesAsync(int connectionId, string tableName)
    {
        // Use AI to generate useful queries based on table schema
        // Example:
        // - SELECT * FROM {table} LIMIT 10
        // - SELECT COUNT(*) FROM {table}
        // - SELECT column, COUNT(*) FROM {table} GROUP BY column
    }
    
    public async Task<QueryAnalysisResult> AnalyzeQueryResultsAsync(string query, DataTable results)
    {
        // Analyze results using AI
        // - Detect patterns
        // - Generate insights
        // - Suggest visualizations
    }
    
    public async Task<ChartRecommendation> RecommendChartsAsync(DataTable results)
    {
        // Based on data types and patterns:
        // - Numeric columns → bar/line charts
        // - Categories + counts → pie charts
        // - Time series → line charts
        // - Multiple metrics → scatter plots
    }
}
```

### PHASE 5: Chart Visualization in Chat
**Status**: Pending
**Files to Update**:
- `Views/Chat/Index.cshtml` - Add chart rendering area
- `wwwroot/js/chat.js` - Add Chart.js integration
- `Controllers/ChartController.cs` - New controller for chart CRUD

**UI Components**:
```html
<!-- In chat message -->
<div class="query-result">
    <div class="result-header">
        <span>Query executed in 245ms</span>
        <button onclick="pinChart(resultId)">📌 Pin</button>
    </div>
    <div class="result-table">
        <!-- Data table -->
    </div>
    <div class="result-charts">
        <canvas id="chart-{resultId}"></canvas>
    </div>
</div>
```

### PHASE 6: Settings Page Integration
**Status**: Pending
**File to Update**: `Views/Account/Settings.cshtml`

**New Section**:
```html
<div class="tab-pane fade" id="dataSources">
    <h5>Data Source Connections</h5>
    <button class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#dataSourceModal">
        <i class="bi bi-plus-circle"></i> Create Data Source
    </button>
    <div id="dataSourcesGrid" class="mt-3">
        <!-- List of connections with test/edit/delete buttons -->
    </div>
</div>
```

### PHASE 7: Report & Dashboard Builder
**Status**: Pending
**New Views**:
- `Views/Report/Create.cshtml`
- `Views/Report/Edit.cshtml`
- `Views/Report/View.cshtml`
- `Views/Report/Public.cshtml` (no auth)
- `Views/Report/Embed.cshtml` (iframe-friendly)
- `Views/Dashboard/Create.cshtml`
- `Views/Dashboard/Edit.cshtml`
- `Views/Dashboard/View.cshtml`

**New Controllers**:
- `Controllers/ReportController.cs`
- `Controllers/DashboardController.cs`

**Features**:
- Drag-and-drop chart positioning
- Grid layout system
- Export to PDF/PNG
- Public URL generation
- Embed code generation
- Real-time data refresh

### PHASE 8: Admin Controls for Embed Management
**Status**: Pending
**File to Update**: `Controllers/OrganizationController.cs`

**New Methods**:
```csharp
// GET: Organization/GetEmbeds
// Lists all active embeds in organization

// POST: Organization/RevokeEmbed
// Revokes embed access (sets IsActive = false)

// GET: Organization/EmbedAudit
// Shows embed usage analytics
```

**UI in Settings**:
```html
<div class="tab-pane fade" id="embedManagement">
    <h5>Embedded Content Management</h5>
    <div id="embedsList">
        <div class="embed-item">
            <span>Report: Q4 Sales</span>
            <span>Created: 2026-04-01</span>
            <span>Views: 1,245</span>
            <button class="btn btn-danger btn-sm">Revoke</button>
        </div>
    </div>
</div>
```

## 🔒 SECURITY IMPLEMENTATION

### JWT Authentication
All data source APIs use JWT:
```csharp
[Authorize]
[Route("api/datasource/{connectionId}")]
public class DataSourceApiController : Controller
{
    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    
    // Validate user has access to connection
    private async Task<bool> ValidateAccessAsync(int connectionId)
    {
        var userId = GetUserId();
        return await _context.DataSourceConnections
            .AnyAsync(c => c.Id == connectionId && 
                          (c.UserId == userId || 
                           c.Organization.OrganizationMembers.Any(m => m.UserId == userId)));
    }
}
```

### Permission Levels
1. **Connection Owner**: Full access
2. **Organization Member**: Read access
3. **Organization Admin**: Full access + revoke embeds
4. **Public Access**: View-only via public token

## 📊 WORKFLOW EXAMPLE

1. **User creates connection**:
   - Settings → Create Data Source
   - Selects MySQL
   - Enters credentials
   - System tests connection
   - Stores encrypted credentials

2. **Agent generates API**:
   - `GET /api/datasource/123/tables` → ["users", "orders", "products"]
   - `GET /api/datasource/123/schema/users` → columns with types

3. **User asks in chat**: "Show me top 10 customers"
   - Agent generates query: `SELECT * FROM customers ORDER BY total_spent DESC LIMIT 10`
   - Executes via API: `POST /api/datasource/123/query`
   - Stores in QueryHistory
   - Returns results + suggested visualizations

4. **Agent creates chart**:
   - Analyzes results
   - Recommends bar chart
   - Generates Chart.js config
   - Displays in chat
   - User clicks "Pin" → Creates ChartDefinition

5. **User creates report**:
   - Reports → Create
   - Adds pinned charts
   - Arranges in grid
   - Saves
   - Generates public URL + embed code

6. **Admin manages embeds**:
   - Settings → Embed Management
   - Views all active embeds
   - Can revoke any embed
   - Views usage statistics

## 🎯 PRIORITY ORDER

1. ✅ Install NuGet packages (DONE)
2. ⏳ Complete connector implementations (IN PROGRESS)
3. ⏳ Create entities and migration
4. ⏳ Build DataSourceApiController
5. ⏳ Implement QueryAgentService
6. ⏳ Add chart visualization to chat
7. ⏳ Build report/dashboard system
8. ⏳ Add admin controls

## 📝 NOTES

- All API endpoints use JWT Bearer authentication
- Query results cached for 15 minutes
- Charts auto-refresh every 5 minutes (configurable)
- Public URLs expire after 30 days (configurable)
- Embed iframes use CSP headers for security
- All queries logged for audit
- Rate limiting: 100 queries per hour per user

## 🚀 DEPLOYMENT CHECKLIST

- [ ] Run migration: `dotnet ef database update`
- [ ] Configure AI service (OpenAI API key)
- [ ] Set up CORS for embed domains
- [ ] Configure CSP headers
- [ ] Enable rate limiting
- [ ] Set up query result caching (Redis recommended)
- [ ] Configure background job for chart refresh
- [ ] Set up monitoring for query performance
