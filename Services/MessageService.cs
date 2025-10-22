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
        // First, update the unread count in the chat document
        var chat = await _context.Chats.Find(c => c.Id == chatId).FirstOrDefaultAsync();
        if (chat != null)
        {
            UpdateDefinition<Chat> chatUpdate;
            if (chat.UserAId == receiverId)
            {
                chatUpdate = Builders<Chat>.Update.Set(c => c.UnreadCountA, 0);
            }
            else if (chat.UserBId == receiverId)
            {
                chatUpdate = Builders<Chat>.Update.Set(c => c.UnreadCountB, 0);
            }
            else
            {
                // receiverId does not match either user in the chat
                return;
            }

            await _context.Chats.UpdateOneAsync(c => c.Id == chatId, chatUpdate);
        }

        // Then, update the status of the messages
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.ChatId, chatId),
            Builders<Message>.Filter.Eq(m => m.ReceiverId, receiverId)
        );
        var update = Builders<Message>.Update.Set(m => m.Status, "read");
        await _context.Messages.UpdateManyAsync(filter, update);
    }
    }
}