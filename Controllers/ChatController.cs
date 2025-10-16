using Involved_Chat.Services;
using Involved_Chat.DTOS;
using Microsoft.AspNetCore.Mvc;

namespace Involved_Chat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly UserService _userService;

        public ChatController(ChatService chatService, UserService userService)
        {
            _chatService = chatService;
            _userService = userService;
        }

        // Create or get an existing chat between two users
        [HttpPost("between/{userAId}/{userBId}")]
        public async Task<IActionResult> GetOrCreateChat(string userAId, string userBId)
        {
            if (string.IsNullOrWhiteSpace(userAId) || string.IsNullOrWhiteSpace(userBId))
                return BadRequest(new { message = "Both userAId and userBId are required", success = false });

            try
            {
                var chat = await _chatService.GetOrCreateChatAsync(userAId, userBId);
                return Ok(new { message = "Chat retrieved", data = chat, success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, success = false });
            }
        }

        // Get all chats for a user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserChats(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { message = "userId is required", success = false });

            var chats = await _chatService.GetUserChatsAsync(userId);

            // Map to chat list DTO with nested otherUser details
            var enriched = new List<ChatListItemDto>(chats.Count);
            foreach (var c in chats)
            {
                var otherUserId = c.UserAId == userId ? c.UserBId : c.UserAId;
                var other = await _userService.GetUserInfoAsync(otherUserId);

                // compute unread count for the current user (if stored per side)
                var unread = c.UserAId == userId ? c.UnreadCountA : c.UnreadCountB;

                // If other user not found, still return minimal info
                var otherDto = other ?? new UserDto
                {
                    Id = otherUserId,
                    Username = string.Empty,
                    Email = string.Empty,
                    CreatedAt = DateTime.MinValue,
                    DisplayName = string.Empty,
                    PhotoURL = null,
                    IsOnline = false,
                    LastSeen = null,
                    Status = null,
                    Contacts = new List<string>(),
                    ConnectionIds = new List<string>(),
                    About = null,
                    BlockedUsers = new List<string>()
                };

                enriched.Add(new ChatListItemDto
                {
                    ChatId = c.Id,
                    OtherUser = otherDto,
                    LastMessage = c.LastMessage,
                    LastMessageTime = c.LastMessageTime,
                    LastMessageSenderId = c.LastMessageSenderId,
                    UnreadCount = unread
                });
            }

            return Ok(new { message = "Chats fetched", data = enriched, success = true });
        }
    }
}
