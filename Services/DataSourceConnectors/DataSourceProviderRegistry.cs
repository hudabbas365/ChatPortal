using ChatPortal.Services.DataSourceConnectors.SQLConnectors;
using ChatPortal.Services.DataSourceConnectors.NoSQLConnectors;
using ChatPortal.Services.DataSourceConnectors.CloudStorageConnectors;

namespace ChatPortal.Services.DataSourceConnectors
{
    public class DataSourceProvider
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public Type ConnectorType { get; set; } = null!;
        public bool RequiresOAuth { get; set; }
        public string? DocumentationUrl { get; set; }
    }

    public static class DataSourceProviderRegistry
    {
        public static List<DataSourceProvider> GetAllProviders()
        {
            return new List<DataSourceProvider>
            {
                // SQL Databases
                new DataSourceProvider
                {
                    Id = "mysql",
                    Name = "MySQL",
                    Category = "SQL Databases",
                    Description = "Connect to MySQL databases",
                    Icon = "database-fill",
                    ConnectorType = typeof(MySQLConnector),
                    RequiresOAuth = false,
                    DocumentationUrl = "https://www.mysql.com/"
                },
                new DataSourceProvider
                {
                    Id = "postgresql",
                    Name = "PostgreSQL",
                    Category = "SQL Databases",
                    Description = "Connect to PostgreSQL databases",
                    Icon = "database-fill",
                    ConnectorType = typeof(PostgreSQLConnector),
                    RequiresOAuth = false,
                    DocumentationUrl = "https://www.postgresql.org/"
                },
                new DataSourceProvider
                {
                    Id = "sqlserver",
                    Name = "Microsoft SQL Server",
                    Category = "SQL Databases",
                    Description = "Connect to SQL Server and Azure SQL",
                    Icon = "database-fill",
                    ConnectorType = typeof(SqlServerConnector),
                    RequiresOAuth = false,
                    DocumentationUrl = "https://www.microsoft.com/sql-server/"
                },
                new DataSourceProvider
                {
                    Id = "oracle",
                    Name = "Oracle Database",
                    Category = "SQL Databases",
                    Description = "Connect to Oracle databases",
                    Icon = "database-fill",
                    ConnectorType = typeof(SqlServerConnector), // Placeholder - implement OracleConnector
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "db2",
                    Name = "IBM Db2",
                    Category = "SQL Databases",
                    Description = "Connect to IBM Db2 databases",
                    Icon = "database-fill",
                    ConnectorType = typeof(SqlServerConnector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "snowflake",
                    Name = "Snowflake",
                    Category = "SQL Databases",
                    Description = "Connect to Snowflake data warehouse",
                    Icon = "cloud-snow",
                    ConnectorType = typeof(SqlServerConnector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "amazonrds",
                    Name = "Amazon RDS",
                    Category = "SQL Databases",
                    Description = "Connect to Amazon RDS databases",
                    Icon = "database-fill",
                    ConnectorType = typeof(MySQLConnector), // Can use MySQL/PostgreSQL connectors
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "redshift",
                    Name = "Amazon Redshift",
                    Category = "SQL Databases",
                    Description = "Connect to Amazon Redshift data warehouse",
                    Icon = "database-fill",
                    ConnectorType = typeof(PostgreSQLConnector), // Uses PostgreSQL protocol
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "bigquery",
                    Name = "Google BigQuery",
                    Category = "SQL Databases",
                    Description = "Connect to Google BigQuery",
                    Icon = "google",
                    ConnectorType = typeof(SqlServerConnector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "teradata",
                    Name = "Teradata",
                    Category = "SQL Databases",
                    Description = "Connect to Teradata databases",
                    Icon = "database-fill",
                    ConnectorType = typeof(SqlServerConnector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "saphana",
                    Name = "SAP HANA",
                    Category = "SQL Databases",
                    Description = "Connect to SAP HANA databases",
                    Icon = "database-fill",
                    ConnectorType = typeof(SqlServerConnector), // Placeholder
                    RequiresOAuth = false
                },

                // NoSQL Databases
                new DataSourceProvider
                {
                    Id = "mongodb",
                    Name = "MongoDB Atlas",
                    Category = "NoSQL Databases",
                    Description = "Connect to MongoDB databases",
                    Icon = "database",
                    ConnectorType = typeof(MongoDBConnector),
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "cosmosdb",
                    Name = "Azure Cosmos DB",
                    Category = "NoSQL Databases",
                    Description = "Connect to Azure Cosmos DB",
                    Icon = "cloud-fill",
                    ConnectorType = typeof(MongoDBConnector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "dynamodb",
                    Name = "Amazon DynamoDB",
                    Category = "NoSQL Databases",
                    Description = "Connect to AWS DynamoDB",
                    Icon = "table",
                    ConnectorType = typeof(MongoDBConnector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "cassandra",
                    Name = "Apache Cassandra",
                    Category = "NoSQL Databases",
                    Description = "Connect to Cassandra databases",
                    Icon = "database",
                    ConnectorType = typeof(MongoDBConnector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "firestore",
                    Name = "Google Firestore",
                    Category = "NoSQL Databases",
                    Description = "Connect to Google Firestore",
                    Icon = "google",
                    ConnectorType = typeof(MongoDBConnector), // Placeholder
                    RequiresOAuth = true
                },

                // Cloud Storage
                new DataSourceProvider
                {
                    Id = "s3",
                    Name = "Amazon S3",
                    Category = "Cloud Storage",
                    Description = "Connect to Amazon S3 buckets",
                    Icon = "cloud-upload",
                    ConnectorType = typeof(S3Connector),
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "azureblob",
                    Name = "Azure Blob Storage",
                    Category = "Cloud Storage",
                    Description = "Connect to Azure Blob Storage",
                    Icon = "cloud-fill",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "sharepoint",
                    Name = "SharePoint Online",
                    Category = "Cloud Storage",
                    Description = "Connect to SharePoint document libraries",
                    Icon = "microsoft",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "onedrive",
                    Name = "OneDrive",
                    Category = "Cloud Storage",
                    Description = "Connect to Microsoft OneDrive",
                    Icon = "microsoft",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "googledrive",
                    Name = "Google Drive",
                    Category = "Cloud Storage",
                    Description = "Connect to Google Drive",
                    Icon = "google",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "googlesheets",
                    Name = "Google Sheets",
                    Category = "Cloud Storage",
                    Description = "Connect to Google Sheets",
                    Icon = "google",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "dropbox",
                    Name = "Dropbox",
                    Category = "Cloud Storage",
                    Description = "Connect to Dropbox",
                    Icon = "cloud-arrow-up",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "box",
                    Name = "Box",
                    Category = "Cloud Storage",
                    Description = "Connect to Box storage",
                    Icon = "box",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },

                // CRM & ERP
                new DataSourceProvider
                {
                    Id = "salesforce",
                    Name = "Salesforce",
                    Category = "CRM & ERP",
                    Description = "Connect to Salesforce CRM",
                    Icon = "diagram-3",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "dynamics365",
                    Name = "Dynamics 365",
                    Category = "CRM & ERP",
                    Description = "Connect to Microsoft Dynamics 365",
                    Icon = "microsoft",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "saperp",
                    Name = "SAP ERP / BW",
                    Category = "CRM & ERP",
                    Description = "Connect to SAP systems",
                    Icon = "building",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "netsuite",
                    Name = "Oracle NetSuite",
                    Category = "CRM & ERP",
                    Description = "Connect to Oracle NetSuite",
                    Icon = "building",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "workday",
                    Name = "Workday",
                    Category = "CRM & ERP",
                    Description = "Connect to Workday HCM",
                    Icon = "people",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "servicenow",
                    Name = "ServiceNow",
                    Category = "CRM & ERP",
                    Description = "Connect to ServiceNow",
                    Icon = "gear",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "jira",
                    Name = "Jira",
                    Category = "CRM & ERP",
                    Description = "Connect to Atlassian Jira",
                    Icon = "kanban",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "zendesk",
                    Name = "Zendesk",
                    Category = "CRM & ERP",
                    Description = "Connect to Zendesk support",
                    Icon = "chat",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },

                // Analytics & Marketing
                new DataSourceProvider
                {
                    Id = "googleanalytics",
                    Name = "Google Analytics",
                    Category = "Analytics & Marketing",
                    Description = "Connect to Google Analytics",
                    Icon = "graph-up",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "adobeanalytics",
                    Name = "Adobe Analytics",
                    Category = "Analytics & Marketing",
                    Description = "Connect to Adobe Analytics",
                    Icon = "graph-up",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "hubspot",
                    Name = "HubSpot",
                    Category = "Analytics & Marketing",
                    Description = "Connect to HubSpot CRM and Marketing",
                    Icon = "envelope",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "marketo",
                    Name = "Marketo",
                    Category = "Analytics & Marketing",
                    Description = "Connect to Adobe Marketo",
                    Icon = "envelope",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "mailchimp",
                    Name = "Mailchimp",
                    Category = "Analytics & Marketing",
                    Description = "Connect to Mailchimp",
                    Icon = "envelope",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "linkedinads",
                    Name = "LinkedIn Ads",
                    Category = "Analytics & Marketing",
                    Description = "Connect to LinkedIn Advertising",
                    Icon = "linkedin",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "facebookads",
                    Name = "Facebook Ads",
                    Category = "Analytics & Marketing",
                    Description = "Connect to Facebook Advertising",
                    Icon = "facebook",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "twitterads",
                    Name = "Twitter Ads",
                    Category = "Analytics & Marketing",
                    Description = "Connect to Twitter Advertising",
                    Icon = "twitter",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },

                // APIs & Web Services
                new DataSourceProvider
                {
                    Id = "restapi",
                    Name = "REST API",
                    Category = "APIs & Web Services",
                    Description = "Connect to any REST API endpoint",
                    Icon = "cloud-arrow-down",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "graphql",
                    Name = "GraphQL API",
                    Category = "APIs & Web Services",
                    Description = "Connect to GraphQL endpoints",
                    Icon = "cloud-arrow-down",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "soap",
                    Name = "SOAP API",
                    Category = "APIs & Web Services",
                    Description = "Connect to SOAP web services",
                    Icon = "cloud-arrow-down",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },

                // Streaming & Events
                new DataSourceProvider
                {
                    Id = "kafka",
                    Name = "Apache Kafka",
                    Category = "Streaming & Events",
                    Description = "Connect to Kafka streams",
                    Icon = "arrow-repeat",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "rabbitmq",
                    Name = "RabbitMQ",
                    Category = "Streaming & Events",
                    Description = "Connect to RabbitMQ message broker",
                    Icon = "arrow-repeat",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "eventhub",
                    Name = "Azure Event Hub",
                    Category = "Streaming & Events",
                    Description = "Connect to Azure Event Hub",
                    Icon = "cloud-fill",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "kinesis",
                    Name = "AWS Kinesis",
                    Category = "Streaming & Events",
                    Description = "Connect to AWS Kinesis streams",
                    Icon = "arrow-repeat",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "iothub",
                    Name = "Azure IoT Hub",
                    Category = "Streaming & Events",
                    Description = "Connect to Azure IoT Hub",
                    Icon = "router",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "mqtt",
                    Name = "MQTT Broker",
                    Category = "Streaming & Events",
                    Description = "Connect to MQTT message brokers",
                    Icon = "router",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },

                // Files & Data Feeds
                new DataSourceProvider
                {
                    Id = "excel",
                    Name = "Excel Online",
                    Category = "Files & Data Feeds",
                    Description = "Connect to Excel files in Office 365",
                    Icon = "file-earmark-excel",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "csv",
                    Name = "CSV/JSON/XML Files",
                    Category = "Files & Data Feeds",
                    Description = "Connect to structured file formats",
                    Icon = "file-earmark-text",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "odata",
                    Name = "OData Feeds",
                    Category = "Files & Data Feeds",
                    Description = "Connect to OData endpoints",
                    Icon = "rss",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "hadoop",
                    Name = "Hadoop/HDFS",
                    Category = "Files & Data Feeds",
                    Description = "Connect to Hadoop file system",
                    Icon = "hdd-stack",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },

                // Financial Data
                new DataSourceProvider
                {
                    Id = "github",
                    Name = "GitHub",
                    Category = "Developer Tools",
                    Description = "Connect to GitHub repositories",
                    Icon = "github",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = true
                },
                new DataSourceProvider
                {
                    Id = "bloomberg",
                    Name = "Bloomberg",
                    Category = "Financial Data",
                    Description = "Connect to Bloomberg financial data",
                    Icon = "cash",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "refinitiv",
                    Name = "Refinitiv",
                    Category = "Financial Data",
                    Description = "Connect to Refinitiv data feeds",
                    Icon = "cash",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                },
                new DataSourceProvider
                {
                    Id = "yahoofinance",
                    Name = "Yahoo Finance",
                    Category = "Financial Data",
                    Description = "Connect to Yahoo Finance APIs",
                    Icon = "graph-up-arrow",
                    ConnectorType = typeof(S3Connector), // Placeholder
                    RequiresOAuth = false
                }
            };
        }

        public static Dictionary<string, List<DataSourceProvider>> GetProvidersByCategory()
        {
            return GetAllProviders()
                .GroupBy(p => p.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}
