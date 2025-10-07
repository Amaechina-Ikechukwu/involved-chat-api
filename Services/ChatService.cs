using Involved_Chat.Data;
using Involved_Chat.Models;
using MongoDB.Driver;

namespace Involved_Chat.Services
{
    public class ChatService
    {

    private readonly MongoDbContext _context;

    public ChatService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Chat> GetOrCreateChatAsync(string userAId, string userBId)
    {
        string chatId = GenerateChatId(userAId, userBId);
        var chat = await _context.Chats.Find(c => c.Id == chatId).FirstOrDefaultAsync();

        if (chat == null)
        {
            chat = new Chat
            {
                Id = chatId,
                UserAId = userAId,
                UserBId = userBId,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Chats.InsertOneAsync(chat);
        }

        return chat;
    }

    public static string GenerateChatId(string a, string b)
    {
        var ordered = new[] { a, b }.OrderBy(x => x);
        return string.Join("_", ordered);
    }

    public async Task UpdateChatPreviewAsync(string chatId, string senderId, string message, DateTime time)
    {
        var update = Builders<Chat>.Update
            .Set(c => c.LastMessage, message)
            .Set(c => c.LastMessageTime, time)
            .Set(c => c.LastMessageSenderId, senderId);

        await _context.Chats.UpdateOneAsync(c => c.Id == chatId, update);
    }

    public async Task<List<Chat>> GetUserChatsAsync(string userId)
    {
        var filter = Builders<Chat>.Filter.Or(
            Builders<Chat>.Filter.Eq(c => c.UserAId, userId),
            Builders<Chat>.Filter.Eq(c => c.UserBId, userId)
        );

        return await _context.Chats.Find(filter)
            .SortByDescending(c => c.LastMessageTime)
            .ToListAsync();
    }
}
    
    }
