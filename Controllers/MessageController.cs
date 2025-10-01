using System.Security.Claims;
using Involved_Chat.Services;
using Microsoft.AspNetCore.Mvc;

namespace Involved_Chat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]


    public class MessageController : ControllerBase
    {
        private readonly MessageService _messageService;

        public MessageController(
            MessageService messageService
        )
        {
            _messageService = messageService;
        }

        [HttpPost("conversations/{userId}")]
        public async Task<IActionResult> GetConversationsAsync(string userId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
            {
                return Unauthorized();
            }
            var messages = await _messageService.GetConversations(currentUserId, userId);
            return Ok(messages);
        }
    }
}