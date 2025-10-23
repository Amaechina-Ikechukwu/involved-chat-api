using Involved_Chat.Data;
using Involved_Chat.Models;
using Involved_Chat.DTOS;
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
        var chat = await _context.Chats.Find(c => c.ChatKey == chatId).FirstOrDefaultAsync();

        if (chat == null)
        {
            chat = new Chat
            {
                ChatKey = chatId,
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

    public async Task UpdateChatPreviewAsync(string chatId, string senderId, string receiverId, string message, DateTime time)
    {
        var chat = await _context.Chats.Find(c => c.Id == chatId).FirstOrDefaultAsync();
        if (chat == null) return;

        var updateDefinition = Builders<Chat>.Update
            .Set(c => c.LastMessage, message)
            .Set(c => c.LastMessageTime, time)
            .Set(c => c.LastMessageSenderId, senderId);

        if (chat.UserAId == receiverId)
        {
            updateDefinition = updateDefinition.Inc(c => c.UnreadCountA, 1);
        }
        else if (chat.UserBId == receiverId)
        {
            updateDefinition = updateDefinition.Inc(c => c.UnreadCountB, 1);
        }

        await _context.Chats.UpdateOneAsync(c => c.Id == chatId, updateDefinition);
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

    // Returns chat list enriched with the other participant's user info and unread counts
    public async Task<List<ChatListItemDto>> GetUserChatsWithDetailsAsync(string userId)
    {
        // 1) Load chats for user
        var chats = await GetUserChatsAsync(userId);
        if (chats.Count == 0)
        {
            return new List<ChatListItemDto>();
        }

        // 2) Collect other user ids and load all at once (avoid N+1)
        // Prefer deriving from ChatKey (sorted "userA_userB") to avoid any stale/corrupted UserAId/UserBId values.
        var otherUserIds = chats
            .Select(c =>
            {
                // If ChatKey exists, derive the other participant from it
                if (!string.IsNullOrWhiteSpace(c.ChatKey) && c.ChatKey.Contains("_"))
                {
                    var parts = c.ChatKey.Split('_');
                    // parts are sorted; pick the one that's not the current user
                    return parts[0] == userId ? parts[1] : (parts[1] == userId ? parts[0] : parts[0]);
                }

                // Fallback to stored fields
                return c.UserAId == userId ? c.UserBId : c.UserAId;
            })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        
        foreach (var chat in chats)
        {
            Console.WriteLine($"[DEBUG] Chat {chat.Id}: ChatKey={chat.ChatKey}, UserAId={chat.UserAId}, UserBId={chat.UserBId}");
        }

        // Fetch all users in a single query using $in operator
        var userFilter = Builders<User>.Filter.In(u => u.Id, otherUserIds);
        var users = await _context.Users.Find(userFilter).ToListAsync();
        
        Console.WriteLine($"[DEBUG] Found {users.Count} users in database");
        foreach (var user in users)
        {
            Console.WriteLine($"[DEBUG] Found user: {user.Id} - {user.DisplayName}");
        }

        var userMap = users.ToDictionary(u => u.Id, u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            DisplayName = u.DisplayName,
            PhotoURL = u.PhotoURL,
            IsOnline = u.IsOnline,
            LastSeen = u.LastSeen,
            Status = u.Status,
           
            About = u.About,
        });

        // 3) Build response
        var result = new List<ChatListItemDto>(chats.Count);
        foreach (var c in chats)
        {
            // Derive other participant robustly
            string otherId;
            if (!string.IsNullOrWhiteSpace(c.ChatKey) && c.ChatKey.Contains("_"))
            {
                var parts = c.ChatKey.Split('_');
                otherId = parts[0] == userId ? parts[1] : (parts[1] == userId ? parts[0] : parts[0]);
            }
            else
            {
                otherId = c.UserAId == userId ? c.UserBId : c.UserAId;
            }
            var unread = c.UserAId == userId ? c.UnreadCountA : c.UnreadCountB;

            userMap.TryGetValue(otherId, out var otherUserDto);
            if (otherUserDto == null)
            {
                // As a safety fallback (in case of legacy/corrupted chat docs), try to infer the peer from message history
                // This avoids returning an empty user when messages exist with valid sender/receiver ids.
                var lastMessage = await _context.Messages
                    .Find(m => m.ChatId == c.Id)
                    .SortByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                var inferredOtherId = lastMessage == null
                    ? otherId
                    : (lastMessage.SenderId == userId ? lastMessage.ReceiverId : lastMessage.SenderId);

                var inferredUser = await _context.Users.Find(u => u.Id == inferredOtherId).FirstOrDefaultAsync();
                otherUserDto = inferredUser != null
                    ? new UserDto
                    {
                        Id = inferredUser.Id,
                        Username = inferredUser.Username,
                        Email = inferredUser.Email,
                       
                        DisplayName = inferredUser.DisplayName,
                        PhotoURL = inferredUser.PhotoURL,
                        IsOnline = inferredUser.IsOnline,
                        LastSeen = inferredUser.LastSeen,
                        Status = inferredUser.Status,
                       
                    }
                    : new UserDto
                    {
                        Id = inferredOtherId,
                        Username = string.Empty,
                        Email = string.Empty,
                        DisplayName = string.Empty,
                        PhotoURL = null,
                        IsOnline = false,
                        LastSeen = null,
                        Status = null,
                       
                        Location = null,
                        About = null,
                       
                    };
            }

            result.Add(new ChatListItemDto
            {
                ChatId = c.Id,
                OtherUser = otherUserDto,
                LastMessage = c.LastMessage,
                LastMessageTime = c.LastMessageTime,
                LastMessageSenderId = c.LastMessageSenderId,
                UnreadCount = unread
            });
        }

        return result;
    }

    public async Task<int> GetTotalUnreadCountAsync(string userId)
    {
        var chats = await GetUserChatsAsync(userId);
        return chats.Sum(chat => chat.UserAId == userId ? chat.UnreadCountA : chat.UnreadCountB);
    }

    public async Task<Chat> GetChatByIdAsync(string chatId)
    {
        return await _context.Chats.Find(c => c.Id == chatId).FirstOrDefaultAsync();
    }
}
    
    }
