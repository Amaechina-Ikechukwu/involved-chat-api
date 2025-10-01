using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Involved_Chat.Models;

namespace Involved_Chat.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> options, IMongoClient client)
        {
            _database = client.GetDatabase(options.Value.DatabaseName);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<Message> Messages => _database.GetCollection<Message>("Messages");
    }

    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
    }
}
