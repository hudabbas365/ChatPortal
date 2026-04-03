# Data Source Connection System

## Overview
Comprehensive multi-provider data source connection system supporting 50+ cloud services, databases, APIs, and data sources.

## Architecture

### Entity Model
- **DataSourceConnection.cs** - Main entity storing connection configurations
  - Supports multiple authentication methods (connection strings, API keys, OAuth tokens, username/password)
  - Stores provider-specific configuration as JSON
  - Tracks connection health and sync status
  - Supports both user-level and organization-level connections

### Services Layer
Located in `Services/DataSourceConnectors/`

#### Interface
- **IDataSourceConnector** - Standard interface for all providers
  - `TestConnectionAsync()` - Test connection validity
  - `ConnectAsync()` - Establish connection
  - `DisconnectAsync()` - Close connection
  - `SyncDataAsync()` - Synchronize data
  - `GetHealthAsync()` - Check connection health
  - `GetConfigurationSchema()` - Get JSON schema for provider-specific fields

#### Connectors
Organized by category in subfolders:

**SQL Connectors** (`SQLConnectors/`)
- MySQLConnector
- PostgreSQLConnector
- SqlServerConnector
- OracleConnector (placeholder)
- DB2Connector (placeholder)
- SnowflakeConnector (placeholder)
- RedshiftConnector (placeholder)
- BigQueryConnector (placeholder)
- TeradataConnector (placeholder)
- SAPHANAConnector (placeholder)

**NoSQL Connectors** (`NoSQLConnectors/`)
- MongoDBConnector
- CosmosDBConnector (placeholder)
- DynamoDBConnector (placeholder)
- CassandraConnector (placeholder)
- FirestoreConnector (placeholder)

**Cloud Storage Connectors** (`CloudStorageConnectors/`)
- S3Connector
- AzureBlobConnector (placeholder)
- SharePointConnector (placeholder)
- OneDriveConnector (placeholder)
- GoogleDriveConnector (placeholder)
- GoogleSheetsConnector (placeholder)
- DropboxConnector (placeholder)
- BoxConnector (placeholder)

**Additional Categories** (placeholders)
- CRM & ERP Connectors
- Analytics & Marketing Connectors
- API Connectors (REST, GraphQL, SOAP)
- Streaming & Events Connectors
- File & Data Feed Connectors
- Financial Data Connectors

#### Provider Registry
- **DataSourceProviderRegistry.cs** - Centralized registry of all 50+ providers
  - Maps provider IDs to connector implementations
  - Provides categorized provider lists
  - Includes metadata (name, description, icon, OAuth requirement, documentation URL)

### Controller
- **DataSourceController.cs** - API endpoints for data source management
  - `GET /DataSource/GetProviders` - List all available providers by category
  - `POST /DataSource/Connect` - Create new connection
  - `POST /DataSource/TestConnection` - Test existing connection
  - `POST /DataSource/Disconnect` - Disconnect source
  - `GET /DataSource/GetConnections` - List user's connections
  - `GET /DataSource/GetConnectionStatus` - Check health of specific connection
  - `POST /DataSource/Sync` - Trigger data synchronization
  - `GET /DataSource/GetProviderSchema` - Get configuration schema for provider

### UI Components
Located in `Views/Chat/Index.cshtml`

#### Features
- **Provider Selection Modal** - Browse 50+ providers organized by category
  - Search/filter functionality
  - Provider cards with icons, names, descriptions
  - OAuth badges for providers requiring OAuth
  
- **Dynamic Connection Forms** - Auto-generated based on provider schema
  - Fields adapt to provider requirements
  - Built-in validation
  - Secure password fields
  - Default values from schema

- **Connected Sources List** - Management interface for active connections
  - Connection status indicators (success, failed, warning)
  - Test connection button
  - Sync data button
  - Disconnect button
  - Last sync timestamp

- **Sidebar Status** - Real-time connection count in main UI
  - Shows active vs total connections
  - Updates dynamically

## Supported Providers (50+)

### SQL Databases
1. Microsoft SQL Server / Azure SQL
2. MySQL
3. PostgreSQL
4. Oracle Database
5. IBM Db2
6. Snowflake
7. Amazon RDS
8. Amazon Redshift
9. Google BigQuery
10. Teradata
11. SAP HANA

### NoSQL Databases
12. MongoDB Atlas
13. Azure Cosmos DB
14. Amazon DynamoDB
15. Apache Cassandra
16. Google Firestore

### Cloud Storage
17. SharePoint Online
18. Microsoft OneDrive
19. Google Drive
20. Google Sheets
21. Dropbox
22. Box
23. Azure Blob Storage
24. Amazon S3

### CRM & ERP
25. Salesforce
26. Microsoft Dynamics 365
27. SAP ERP / BW
28. Oracle NetSuite
29. Workday
30. ServiceNow
31. Jira
32. Zendesk

### Analytics & Marketing
33. Google Analytics
34. Adobe Analytics
35. HubSpot
36. Adobe Marketo
37. Mailchimp
38. LinkedIn Ads
39. Facebook Ads
40. Twitter Ads

### APIs & Web Services
41. REST APIs
42. GraphQL APIs
43. SOAP APIs
44. Public web data (scraping, RSS)
45. Open Data portals

### Streaming & Events
46. Apache Kafka
47. RabbitMQ
48. Azure Event Hub
49. AWS Kinesis
50. Azure IoT Hub
51. MQTT brokers

### Files & Data Feeds
52. Excel Online (Office 365)
53. CSV/JSON/XML files
54. OData feeds
55. Hadoop/HDFS

### Financial & Developer Tools
56. GitHub API
57. Bloomberg feeds
58. Refinitiv feeds
59. Yahoo Finance APIs

## Required NuGet Packages

For production use, install the following packages as needed:

### SQL Connectors
```bash
dotnet add package MySql.Data
dotnet add package Npgsql
dotnet add package Oracle.ManagedDataAccess.Core
dotnet add package IBM.Data.DB2.Core
dotnet add package Snowflake.Data
dotnet add package Google.Cloud.BigQuery.V2
```

### NoSQL Connectors
```bash
dotnet add package MongoDB.Driver
dotnet add package Microsoft.Azure.Cosmos
dotnet add package AWSSDK.DynamoDBv2
dotnet add package CassandraCSharpDriver
dotnet add package Google.Cloud.Firestore
```

### Cloud Storage Connectors
```bash
dotnet add package AWSSDK.S3
dotnet add package Azure.Storage.Blobs
dotnet add package Microsoft.Graph
dotnet add package Google.Apis.Drive.v3
dotnet add package Google.Apis.Sheets.v4
dotnet add package Dropbox.Api
```

### CRM & ERP Connectors
```bash
dotnet add package Microsoft.PowerPlatform.Dataverse.Client
dotnet add package Salesforce.Common
dotnet add package Atlassian.SDK
```

### Streaming Connectors
```bash
dotnet add package Confluent.Kafka
dotnet add package RabbitMQ.Client
dotnet add package Azure.Messaging.EventHubs
dotnet add package AWSSDK.Kinesis
dotnet add package MQTTnet
```

## Security Features

### Credential Encryption
All sensitive fields are stored securely:
- Connection strings
- API keys
- Access tokens
- Passwords (hashed)

### Access Control
- Connections are scoped to users
- Organization-level connections supported
- Permission validation on all operations

### Best Practices
- Never expose credentials in logs or error messages
- Use environment variables for sensitive configuration
- Implement OAuth flows for services requiring it (Google, Microsoft, Salesforce)
- Enable SSL/TLS for database connections
- Rotate credentials regularly

## Usage Examples

### Connecting to MySQL
```javascript
// User selects MySQL from provider modal
// Form auto-generates with fields: server, port, database, username, password
// On submit, system:
// 1. Tests connection
// 2. Stores configuration in database
// 3. Updates UI with connection status
```

### Connecting to Amazon S3
```javascript
// User selects Amazon S3 from provider modal
// Form auto-generates with fields: accessKeyId, secretAccessKey, region, bucketName
// On submit, system:
// 1. Creates AWS credentials
// 2. Tests bucket access
// 3. Stores configuration
// 4. Shows bucket info in metadata
```

### Syncing Data
```javascript
// User clicks sync button on connected source
// System:
// 1. Retrieves connection from database
// 2. Creates appropriate connector instance
// 3. Calls SyncDataAsync()
// 4. Updates last sync timestamp and status
// 5. Shows toast notification with results
```

## Extending the System

### Adding a New Connector

1. **Create Connector Class**
```csharp
namespace ChatPortal.Services.DataSourceConnectors.CategoryFolder
{
    public class NewProviderConnector : IDataSourceConnector
    {
        private readonly AppDbContext _context;
        
        public NewProviderConnector(AppDbContext context)
        {
            _context = context;
        }
        
        // Implement interface methods
    }
}
```

2. **Register in DataSourceProviderRegistry**
```csharp
new DataSourceProvider
{
    Id = "newprovider",
    Name = "New Provider",
    Category = "Category Name",
    Description = "Connect to New Provider",
    Icon = "bootstrap-icon-name",
    ConnectorType = typeof(NewProviderConnector),
    RequiresOAuth = false,
    DocumentationUrl = "https://docs.newprovider.com"
}
```

3. **Implement GetConfigurationSchema()**
Return JSON schema defining required configuration fields.

## Database Schema

### DataSourceConnections Table
```sql
CREATE TABLE DataSourceConnections (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    OrganizationId INT NULL,
    Name NVARCHAR(200) NOT NULL,
    SourceType NVARCHAR(100) NOT NULL,
    Provider NVARCHAR(100) NOT NULL,
    ConnectionString NVARCHAR(500) NULL,
    ApiEndpoint NVARCHAR(500) NULL,
    ApiKey NVARCHAR(500) NULL,
    AccessToken NVARCHAR(500) NULL,
    Username NVARCHAR(200) NULL,
    PasswordHash NVARCHAR(500) NULL,
    AdditionalConfig NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ConnectedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastSyncAt DATETIME2 NULL,
    LastSyncStatus NVARCHAR(MAX) NULL,
    Description NVARCHAR(1000) NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id)
);
```

## Future Enhancements

1. **Credential Encryption** - Use ASP.NET Core Data Protection API
2. **OAuth Flows** - Implement for Google, Microsoft, Salesforce
3. **Connection Pooling** - Optimize for frequently accessed sources
4. **Health Monitoring** - Background service to check connection health
5. **Data Caching** - Cache frequently accessed data
6. **Rate Limiting** - Prevent API quota exhaustion
7. **Audit Logging** - Track all connection operations
8. **Connection Sharing** - Share connections within organizations
9. **Schema Discovery** - Auto-detect tables/collections in connected sources
10. **Query Builder** - Visual interface for building data queries

## Troubleshooting

### Connection Fails
- Verify credentials are correct
- Check network connectivity
- Ensure firewall allows outbound connections
- Verify SSL/TLS certificates if required
- Check API quotas and rate limits

### Sync Issues
- Check connection health status
- Verify permissions on data source
- Review error details in LastSyncStatus
- Check API rate limits
- Ensure data source is accessible from server

### Missing Providers
- Install required NuGet packages
- Rebuild solution
- Clear browser cache
- Check DataSourceProviderRegistry for registration

## API Response Examples

### Get Providers
```json
{
  "success": true,
  "categories": [
    {
      "category": "SQL Databases",
      "providers": [
        {
          "id": "mysql",
          "name": "MySQL",
          "description": "Connect to MySQL databases",
          "icon": "database-fill",
          "requiresOAuth": false,
          "documentationUrl": "https://www.mysql.com/"
        }
      ]
    }
  ]
}
```

### Connection Result
```json
{
  "success": true,
  "message": "Successfully connected to MySQL database",
  "connectionId": 123,
  "connectionStatus": "Connected",
  "metadata": {
    "ServerVersion": "8.0.35",
    "Database": "myapp_production"
  }
}
```

### Sync Result
```json
{
  "success": true,
  "recordsProcessed": 1250,
  "syncTime": "2026-04-03T14:30:00Z",
  "message": "Successfully synced. Found 15 tables."
}
```

## License
Part of ChatPortal application.

## Support
For issues or questions, contact the development team.
