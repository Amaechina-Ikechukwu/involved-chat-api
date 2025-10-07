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

public async Task<Message> SendMessageAsync(string chatId, string senderId, string receiverId, string content)
    {
        var message = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            SentAt = DateTime.UtcNow,
            Status = "sent"
        };

        await _context.Messages.InsertOneAsync(message);
        return message;
    }

    public async Task<List<Message>> GetMessagesAsync(string chatId, int limit = 50)
    {
        return await _context.Messages.Find(m => m.ChatId == chatId)
            .SortByDescending(m => m.SentAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(string chatId, string receiverId)
    {
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.ChatId, chatId),
            Builders<Message>.Filter.Eq(m => m.ReceiverId, receiverId)
        );
        var update = Builders<Message>.Update.Set(m => m.Status, "read");
        await _context.Messages.UpdateManyAsync(filter, update);
    }
    }
}