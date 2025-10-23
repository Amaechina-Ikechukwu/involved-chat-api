using Involved_Chat.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Involved_Chat.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
      [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
   


    public class MessageController : ControllerBase
    {
        private readonly MessageService _messageService;
        private readonly ChatService _chatService;
        private readonly UserService _userService;

        public MessageController(MessageService messageService, ChatService chatService, UserService userService)
        {
            _messageService = messageService;
            _chatService = chatService;
            _userService = userService;
        }

        // Get conversation between current user and another user
        [HttpGet("conversations/{userBId}")]
        public async Task<IActionResult> GetConversationsAsync(string userBId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (string.IsNullOrWhiteSpace(userBId))
                return BadRequest(new { message = "userBId is required", success = false });

            var chat = await _chatService.GetOrCreateChatAsync(userId, userBId);
            var messages = await _messageService.GetMessagesAsync(chat.Id, userId);
            return Ok(new { message = "Conversations fetched", data = messages, success = true });
        }

        public class SendMessageDto
        {
            public string Content { get; set; } = null!;
        }

        // Send message from current user to another user
        [HttpPost("send/{userBId}")]
        public async Task<IActionResult> SendMessage(string userBId, [FromBody] SendMessageDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (string.IsNullOrWhiteSpace(userBId))
                return BadRequest(new { message = "userBId is required", success = false });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Content is required", success = false });

            var senderId = userId;
            var receiverId = userBId;
            var chat = await _chatService.GetOrCreateChatAsync(senderId, receiverId);

            var message = await _messageService.SendMessageAsync(chat.Id, senderId, receiverId, dto.Content);
            await _chatService.UpdateChatPreviewAsync(chat.Id, senderId, receiverId, dto.Content, message.SentAt);

            // Add both users to each other's contacts
            await _userService.AddContactAsync(senderId, receiverId);

            return Ok(new { message = "Message sent", data = message, success = true });
        }
    }
}