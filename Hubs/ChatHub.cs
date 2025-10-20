using Involved_Chat.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace Involved_Chat.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly MessageService _messageService;
        private readonly ChatService _chatService;
        private readonly UserService _userService;

        public ChatHub(MessageService messageService, ChatService chatService, UserService userService)
        {
            _messageService = messageService;
            _chatService = chatService;
            _userService = userService;
        }

        // User connects to SignalR
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
                await _userService.UpdateStatusAsync(userId, true, null, connectionId);

            await base.OnConnectedAsync();
        }

        //User disconnects
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
                await _userService.UpdateStatusAsync(userId, false, DateTime.UtcNow, connectionId);

            await base.OnDisconnectedAsync(exception);
        }

        //Send message
        public async Task SendMessage(string receiverId, string content)
        {
            var senderId = Context.UserIdentifier;
            if (senderId == null)
                return;

            //Get or create chat between sender and receiver
            var chat = await _chatService.GetOrCreateChatAsync(senderId, receiverId);

            //Save message to database
            var message = await _messageService.SendMessageAsync(chat.Id, senderId, receiverId, content);

            //Update chat preview
            await _chatService.UpdateChatPreviewAsync(chat.Id, senderId, content, message.SentAt);

           

            // 5️⃣ Notify sender (delivery confirmation)
            await Clients.Caller.SendAsync("MessageDelivered", message.Id);
        }

        // Mark messages as read
        public async Task MarkAsRead(string chatId)
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
                return;

            await _messageService.MarkAsReadAsync(chatId, userId);
            await Clients.Caller.SendAsync("MessagesRead", chatId);
        }

        // Fetch user’s chat list (for chat overview screen)
        public async Task<List<object>> GetChatList()
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
                return new List<object>();

            var chats = await _chatService.GetUserChatsAsync(userId);

            return chats.Select(c => new
            {
                chatId = c.Id,
                lastMessage = c.LastMessage,
                lastMessageTime = c.LastMessageTime,
                lastMessageSenderId = c.LastMessageSenderId,
                receiverId = c.UserAId == userId ? c.UserBId : c.UserAId
            }).Cast<object>().ToList();
        }
    }
}
