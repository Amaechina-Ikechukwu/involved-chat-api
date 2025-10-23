using Involved_Chat.Data;
using Involved_Chat.DTOS;
using Involved_Chat.Models;
using MongoDB.Driver;
using System.Linq;

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

        public async Task<GetMessagesResponseDto> GetMessagesAsync(string chatId, string currentUserId, int limit = 50)
        {
            // 1. Find the chat to identify the other user
            var chat = await _context.Chats.Find(c => c.Id == chatId).FirstOrDefaultAsync();
            if (chat == null)
            {
                // Avoid nullable returns; callers can catch and respond appropriately
                throw new KeyNotFoundException("Chat not found");
            }

            var otherUserId = chat.UserAId == currentUserId ? chat.UserBId : chat.UserAId;

            // 2. Find the other user's details
            var otherUser = await _context.Users.Find(u => u.Id == otherUserId).FirstOrDefaultAsync();
            var otherUserDto = otherUser != null
                ? new UserDto
                {
                    Id = otherUser.Id,
                    Username = otherUser.Username,
                    Email = otherUser.Email,
                    CreatedAt = otherUser.CreatedAt,
                    DisplayName = otherUser.DisplayName,
                    PhotoURL = otherUser.PhotoURL,
                    IsOnline = otherUser.IsOnline,
                    LastSeen = otherUser.LastSeen,
                    Status = otherUser.Status,
                    Contacts = otherUser.Contacts,
                    ConnectionIds = otherUser.ConnectionIds,
                    PushTokens = otherUser.PushTokens,
                    Location = otherUser.Location == null ? null : new LocationDto
                    {
                        Latitude = otherUser.Location.Latitude,
                        Longitude = otherUser.Location.Longitude
                    },
                    About = otherUser.About,
                    BlockedUsers = otherUser.BlockedUsers
                }
                : new UserDto
                {
                    Id = otherUserId,
                    Username = string.Empty,
                    Email = string.Empty,
                    CreatedAt = DateTime.MinValue,
                    DisplayName = "Unknown user",
                    PhotoURL = null,
                    IsOnline = false,
                    LastSeen = null,
                    Status = null,
                    Contacts = new List<string>(),
                    ConnectionIds = new List<string>(),
                    PushTokens = new List<string>(),
                    Location = null,
                    About = null,
                    BlockedUsers = new List<string>()
                };

            // 3. Find all users in the chat to efficiently get sender info
            var userIds = new[] { chat.UserAId, chat.UserBId };
            // Use MongoDB filter builder for better compatibility
            var userFilter = Builders<User>.Filter.In(u => u.Id, userIds);
            var usersInChat = await _context.Users.Find(userFilter).ToListAsync();
            var userDtos = usersInChat.ToDictionary(u => u.Id, u => new UserDto
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
                PushTokens = u.PushTokens,
                Location = u.Location == null ? null : new LocationDto
                {
                    Latitude = u.Location.Latitude,
                    Longitude = u.Location.Longitude
                },
                About = u.About,
                BlockedUsers = u.BlockedUsers
            });

            // 4. Find the messages and join with sender info
            var messages = await _context.Messages.Find(m => m.ChatId == chatId)
                .SortByDescending(m => m.SentAt)
                .Limit(limit)
                .ToListAsync();

            var messageDtos = messages.Select(m =>
            {
                var hasSender = userDtos.TryGetValue(m.SenderId, out var senderDto);
                var fallbackSender = hasSender
                    ? senderDto!
                    : new UserDto
                    {
                        Id = m.SenderId,
                        Username = string.Empty,
                        Email = string.Empty,
                        CreatedAt = DateTime.MinValue,
                        DisplayName = "Unknown user",
                        PhotoURL = null,
                        IsOnline = false,
                        LastSeen = null,
                        Status = null,
                        Contacts = new List<string>(),
                        ConnectionIds = new List<string>(),
                        PushTokens = new List<string>(),
                        Location = null,
                        About = null,
                        BlockedUsers = new List<string>()
                    };

                return new MessageDto
                {
                    Id = m.Id,
                    ChatId = m.ChatId,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    Status = m.Status,
                    Sender = fallbackSender
                };
            }).ToList();

            // 5. Create and return the response DTO
            return new GetMessagesResponseDto
            {
                Messages = messageDtos,
                OtherUser = otherUserDto
            };
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