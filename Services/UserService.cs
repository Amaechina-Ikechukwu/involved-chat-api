using Involved_Chat.Data;
using Involved_Chat.Models;
using Involved_Chat.DTOS;
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

        public async Task UpdatePhotoUrlAsync(string userId, string photoUrl)
        {
            var update = Builders<User>.Update.Set(u => u.PhotoURL, photoUrl);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task UpdateAboutAsync(string userId, string about)
        {
            var update = Builders<User>.Update.Set(u => u.About, about);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task BlockUserAsync(string userId, string blockUserId)
        {
            var update = Builders<User>.Update.AddToSet(u => u.BlockedUsers, blockUserId);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task UnblockUserAsync(string userId, string unblockUserId)
        {
            var update = Builders<User>.Update.Pull(u => u.BlockedUsers, unblockUserId);
            await _context.Users.UpdateOneAsync(u => u.Id == userId, update);
        }

        public async Task<UserDto?> GetUserInfoAsync(string userId)
        {
            var projection = Builders<User>.Projection.Expression(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                DisplayName = u.DisplayName,
                PhotoURL = u.PhotoURL,
                IsOnline = u.IsOnline,
                LastSeen = u.LastSeen,
                Status = u.Status,
                Contacts = u.Contacts,
                ConnectionIds = u.ConnectionIds,
                About = u.About,
                BlockedUsers = u.BlockedUsers
            });

            return await _context.Users.Find(u => u.Id == userId).Project(projection).FirstOrDefaultAsync();
        }

        // Return list of user ids the user has exchanged messages with (either sender or receiver)
        public async Task<List<string>> GetContactsAsync(string userId)
        {
            // Query Messages collection for distinct other party ids
            var sentFilter = Builders<Message>.Filter.Eq(m => m.SenderId, userId);
            var receivedFilter = Builders<Message>.Filter.Eq(m => m.ReceiverId, userId);
            var combined = Builders<Message>.Filter.Or(sentFilter, receivedFilter);

            var collection = _context.Messages;

            // Project the other party id per message
            var projection = Builders<Message>.Projection.Expression(m => m.SenderId == userId ? m.ReceiverId : m.SenderId);

            var list = await collection.Find(combined).Project(projection).ToListAsync();

            // distinct and remove nulls/own id if present
            return list.Where(id => !string.IsNullOrEmpty(id) && id != userId).Distinct().ToList();
        }

    }
}