using Involved_Chat.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Involved_Chat.Services
{
    public class MongoHealthCheck : IHealthCheck
    {
        private readonly IMongoDatabase _db;

        public MongoHealthCheck(IConfiguration configuration)
        {
            var connection = configuration["MongoSettings:Connection"];
            var dbName = configuration["MongoSettings:DatabaseName"];
             var client = new MongoClient(connection);
            _db = client.GetDatabase(dbName);
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var isHealthy = await CheckMongoDBConnectionAsync();
            return isHealthy
                ? HealthCheckResult.Healthy("MongoDB is healthy")
                : HealthCheckResult.Unhealthy("MongoDB is not reachable");
        }

        private async Task<bool> CheckMongoDBConnectionAsync()
        {
            try
            {
                await _db.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
