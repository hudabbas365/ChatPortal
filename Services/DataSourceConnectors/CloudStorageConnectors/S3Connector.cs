using ChatPortal.Models.Entities;
using ChatPortal.Data;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using System.Data;
using System.Text.Json;

namespace ChatPortal.Services.DataSourceConnectors.CloudStorageConnectors
{
    public class S3Connector : IDataSourceConnector
    {
        private readonly AppDbContext _context;

        public S3Connector(AppDbContext context)
        {
            _context = context;
        }

        private AmazonS3Client CreateClient(DataSourceConnection connection)
        {
            var credentials = new BasicAWSCredentials(connection.Username!, connection.ApiKey!);

            // Parse region from AdditionalConfig or use default
            var config = new AmazonS3Config();
            if (!string.IsNullOrEmpty(connection.AdditionalConfig))
            {
                try
                {
                    var configData = JsonSerializer.Deserialize<S3ConfigData>(connection.AdditionalConfig);
                    config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(configData?.Region ?? "us-east-1");
                }
                catch
                {
                    config.RegionEndpoint = Amazon.RegionEndpoint.USEast1;
                }
            }
            else
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.USEast1;
            }

            return new AmazonS3Client(credentials, config);
        }

        public async Task<ConnectionResult> TestConnectionAsync(DataSourceConnection connection)
        {
            try
            {
                using var client = CreateClient(connection);
                var response = await client.ListBucketsAsync();

                return new ConnectionResult
                {
                    Success = true,
                    Message = "Successfully connected to Amazon S3",
                    Metadata = new Dictionary<string, object>
                    {
                        { "BucketCount", response.Buckets.Count },
                        { "Owner", response.Owner.DisplayName ?? "Unknown" },
                        { "Region", client.Config.RegionEndpoint.SystemName }
                    }
                };
            }
            catch (AmazonS3Exception ex)
            {
                return new ConnectionResult
                {
                    Success = false,
                    Message = "S3 connection test failed",
                    ErrorDetails = $"AWS Error: {ex.ErrorCode} - {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new ConnectionResult
                {
                    Success = false,
                    Message = "S3 connection test failed",
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
                using var client = CreateClient(connection);
                var response = await client.ListBucketsAsync();

                connection.LastSyncAt = DateTime.UtcNow;
                connection.LastSyncStatus = "Success";
                await _context.SaveChangesAsync();

                return new SyncResult
                {
                    Success = true,
                    RecordsProcessed = response.Buckets.Count,
                    SyncTime = DateTime.UtcNow,
                    Message = $"Synced {response.Buckets.Count} S3 buckets."
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
                    ""accessKeyId"": { ""type"": ""string"", ""description"": ""AWS Access Key ID"" },
                    ""secretAccessKey"": { ""type"": ""string"", ""format"": ""password"", ""description"": ""AWS Secret Access Key"" },
                    ""region"": { ""type"": ""string"", ""default"": ""us-east-1"", ""description"": ""AWS Region"" },
                    ""bucketName"": { ""type"": ""string"", ""description"": ""S3 Bucket Name (optional)"" }
                },
                ""required"": [""accessKeyId"", ""secretAccessKey"", ""region""]
            }";
        }

        public async Task<List<string>> GetTablesAsync(DataSourceConnection connection)
        {
            try
            {
                using var client = CreateClient(connection);
                var response = await client.ListBucketsAsync();
                return response.Buckets.Select(b => b.BucketName).ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public async Task<Dictionary<string, string>> GetTableSchemaAsync(DataSourceConnection connection, string tableName)
        {
            var schema = new Dictionary<string, string>();

            try
            {
                using var client = CreateClient(connection);

                var locationResponse = await client.GetBucketLocationAsync(tableName);
                schema["BucketName"] = tableName;
                schema["Region"] = locationResponse.Location.Value;

                try
                {
                    var versioningResponse = await client.GetBucketVersioningAsync(tableName);
                    schema["Versioning"] = versioningResponse.VersioningConfig.Status.Value;
                }
                catch
                {
                    schema["Versioning"] = "Not configured";
                }

                schema["Note"] = "S3 buckets store objects (files). Use bucket name as query to list objects.";
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
            dt.Columns.Add("Key", typeof(string));
            dt.Columns.Add("Size", typeof(long));
            dt.Columns.Add("LastModified", typeof(DateTime));
            dt.Columns.Add("StorageClass", typeof(string));
            dt.Columns.Add("ETag", typeof(string));

            try
            {
                var bucketName = query.Trim();
                if (string.IsNullOrEmpty(bucketName))
                {
                    throw new ArgumentException("Query must specify a bucket name");
                }

                using var client = CreateClient(connection);
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    MaxKeys = 1000
                };

                var response = await client.ListObjectsV2Async(request);

                foreach (var obj in response.S3Objects)
                {
                    dt.Rows.Add(
                        obj.Key,
                        obj.Size,
                        obj.LastModified,
                        obj.StorageClass.Value,
                        obj.ETag
                    );
                }
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

    public class S3ConfigData
    {
        public string? Region { get; set; }
        public string? BucketName { get; set; }
    }
}
