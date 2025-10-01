using Involved_Chat.Data;
using Involved_Chat.Models;
using MongoDB.Driver;

namespace Involved_Chat.Services
{
    public class MessageService
    {
        private readonly MongoDbContext _context;

        public MessageService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<Message?> SendMessageAsync(string fromUserId, string toUserId, string content)
        {
            var fromExists = await _context.Users.Find(u => u.Id == fromUserId).AnyAsync();
            var toExists = await _context.Users.Find(u => u.Id == toUserId).AnyAsync();

            if (!fromExists || !toExists)
            {
                // return null so callers can respond with 404 or validation error
                return null;
            }

            var message = new Message
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            await _context.Messages.InsertOneAsync(message);
            return message;



        }

        public async Task<List<Message>> GetConversations(string userA, string userB)
        {
            var filter = Builders<Message>.Filter.Or(
               Builders<Message>.Filter.And(
                   Builders<Message>.Filter.Eq(m => m.FromUserId, userA),
                   Builders<Message>.Filter.Eq(m => m.ToUserId, userB)
               ),
               Builders<Message>.Filter.And(
                   Builders<Message>.Filter.Eq(m => m.FromUserId, userB),
                   Builders<Message>.Filter.Eq(m => m.ToUserId, userA)
               )
           );

            return await _context.Messages
                .Find(filter)
                .SortBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(string user, string messageId)
        {
            var update = Builders<Message>.Update.Set(m => m.IsRead, true); //get what you want to update and set it
            await _context.Messages.FindOneAndUpdateAsync(m => m.Id == messageId && m.ToUserId == user, update);
            
        }
    }
}