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

        [HttpPost("conversations/{chatId}")]
        public async Task<IActionResult> GetConversationsAsync(string chatId)
        {
           
            var messages = await _messageService.GetMessagesAsync(chatId);
            return Ok(messages);
        }
    }
}