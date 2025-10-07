using Involved_Chat.Data;
using Involved_Chat.Models;
using MongoDB.Driver;

namespace Involved_Chat.Services
{
    public class UserService
    {
        private readonly MongoDbContext _context;

        public UserService(MongoDbContext context)
        {
            _context = context;
        }
          

        public async Task UpdateStatusAsync(string userId, bool isOnline, DateTime? lastSeen, string connectionId)
{
    var update = Builders<User>.Update
        .Set(u => u.IsOnline, isOnline)
        .Set(u => u.LastSeen, lastSeen ?? DateTime.UtcNow);

    if (isOnline)
        update = update.AddToSet(u => u.ConnectionIds, connectionId);
    else
        update = update.Pull(u => u.ConnectionIds, connectionId);

    await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
}

    }
}