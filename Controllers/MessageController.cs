using Involved_Chat.Services;
using Microsoft.AspNetCore.Mvc;

namespace Involved_Chat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]


    public class MessageController : ControllerBase
    {
        private readonly MessageService _messageService;
        private readonly ChatService _chatService;

        public MessageController(MessageService messageService, ChatService chatService)
        {
            _messageService = messageService;
            _chatService = chatService;
        }

        // Get conversation by userA/userB
        [HttpGet("conversations/{userAId}/{userBId}")]
        public async Task<IActionResult> GetConversationsAsync(string userAId, string userBId)
        {
            if (string.IsNullOrWhiteSpace(userAId) || string.IsNullOrWhiteSpace(userBId))
                return BadRequest(new { message = "Both userAId and userBId are required", success = false });

            var chat = await _chatService.GetOrCreateChatAsync(userAId, userBId);
            var messages = await _messageService.GetMessagesAsync(chat.Id);
            return Ok(new { message = "Conversations fetched", data = messages, success = true });
        }

        public class SendMessageDto
        {
            public string Content { get; set; } = null!;
        }

        // Send message between two users (userA -> userB)
        [HttpPost("send/{userAId}/{userBId}")]
        public async Task<IActionResult> SendMessage(string userAId, string userBId, [FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(userAId) || string.IsNullOrWhiteSpace(userBId))
                return BadRequest(new { message = "Both userAId and userBId are required", success = false });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Content is required", success = false });

            // Sender is always userAId (per route)
            var senderId = userAId;
            var receiverId = userBId;
            var chat = await _chatService.GetOrCreateChatAsync(userAId, userBId);

            var message = await _messageService.SendMessageAsync(chat.Id, senderId, receiverId, dto.Content);
            await _chatService.UpdateChatPreviewAsync(chat.Id, senderId, dto.Content, message.SentAt);

            return Ok(new { message = "Message sent", data = message, success = true });
        }
    }
}