using Involved_Chat.Services;
using Involved_Chat.DTOS;
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
   
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly UserService _userService;

        public ChatController(ChatService chatService, UserService userService)
        {
            _chatService = chatService;
            _userService = userService;
        }

        // Create or get an existing chat between current user and another user
        [HttpPost("between/{userBId}")]
        public async Task<IActionResult> GetOrCreateChat(string userBId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (string.IsNullOrWhiteSpace(userBId))
                return BadRequest(new { message = "userBId is required", success = false });

            try
            {
                var chat = await _chatService.GetOrCreateChatAsync(userId, userBId);
                return Ok(new { message = "Chat retrieved", data = chat, success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, success = false });
            }
        }

        // Get all chats for a user
        [HttpGet("")]
        public async Task<IActionResult> GetUserChats()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            var enriched = await _chatService.GetUserChatsWithDetailsAsync(userId);
            return Ok(new { message = "Chats fetched", data = enriched, success = true });
        }
    }
}
