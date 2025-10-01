using Involved_Chat.Services;
using Microsoft.AspNetCore.SignalR;


namespace Involved_Chat.Hubs
{
public class ChatHub : Hub
{

        private readonly MessageService _messageService;

        public ChatHub(MessageService messageService)
        {
            _messageService = messageService;
        }
        public async Task SendMessage(string toUserId, string message)
        {
            var fromUserId = Context.UserIdentifier;
            if (fromUserId == null)
            {
                return;
            }
            await _messageService.SendMessageAsync(fromUserId, toUserId, message);
            await Clients.All.SendAsync("ReceiveMessage", toUserId, message);
        }
}}