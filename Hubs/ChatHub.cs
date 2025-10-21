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

            // Send message to receiver
            await Clients.User(receiverId).SendAsync("ReceiveMessage", new
            {
                messageId = message.Id,
                chatId = chat.Id,
                senderId = senderId,
                receiverId = receiverId,
                content = content,
                sentAt = message.SentAt
            });

            // Notify both sender and receiver about chat update
            var senderInfo = await _userService.GetUserInfoAsync(senderId);
            var receiverInfo = await _userService.GetUserInfoAsync(receiverId);

            // Send updated chat info to sender
            await Clients.Caller.SendAsync("ChatUpdated", new
            {
                chatId = chat.Id,
                otherUser = receiverInfo != null ? new
                {
                    id = receiverInfo.Id,
                    displayName = receiverInfo.DisplayName,
                    username = receiverInfo.Username,
                    photoURL = receiverInfo.PhotoURL,
                    isOnline = receiverInfo.IsOnline
                } : null,
                lastMessage = content,
                lastMessageTime = message.SentAt,
                lastMessageSenderId = senderId
            });

            // Send updated chat info to receiver
            await Clients.User(receiverId).SendAsync("ChatUpdated", new
            {
                chatId = chat.Id,
                otherUser = senderInfo != null ? new
                {
                    id = senderInfo.Id,
                    displayName = senderInfo.DisplayName,
                    username = senderInfo.Username,
                    photoURL = senderInfo.PhotoURL,
                    isOnline = senderInfo.IsOnline
                } : null,
                lastMessage = content,
                lastMessageTime = message.SentAt,
                lastMessageSenderId = senderId
            });

            // Notify sender (delivery confirmation)
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

        // Fetch and send enriched chat list to client via SignalR
        public async Task GetIndiidualChats()
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("ChatsError", "User not authenticated");
                return;
            }

            try
            {
                var chats = await _chatService.GetUserChatsAsync(userId);

                // Enrich chat data with other user information
                var enrichedChats = new List<object>();
                foreach (var chat in chats)
                {
                    var otherUserId = chat.UserAId == userId ? chat.UserBId : chat.UserAId;
                    var otherUser = await _userService.GetUserInfoAsync(otherUserId);

                    // Calculate unread count for current user
                    var unreadCount = chat.UserAId == userId ? chat.UnreadCountA : chat.UnreadCountB;

                    enrichedChats.Add(new
                    {
                        chatId = chat.Id,
                        otherUser = otherUser != null ? new
                        {
                            id = otherUser.Id,
                            displayName = otherUser.DisplayName,
                            username = otherUser.Username,
                            photoURL = otherUser.PhotoURL,
                            isOnline = otherUser.IsOnline,
                            lastSeen = otherUser.LastSeen,
                            status = otherUser.Status
                        } : new
                        {
                            id = otherUserId,
                            displayName = "Unknown User",
                            username = string.Empty,
                            photoURL = (string?)null,
                            isOnline = false,
                            lastSeen = (DateTime?)null,
                            status = (string?)null
                        },
                        lastMessage = chat.LastMessage,
                        lastMessageTime = chat.LastMessageTime,
                        lastMessageSenderId = chat.LastMessageSenderId,
                        unreadCount = unreadCount
                    });
                }

                // Send enriched chats to the caller
                await Clients.Caller.SendAsync("ReceiveChats", enrichedChats);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ChatsError", $"Error fetching chats: {ex.Message}");
            }
        }

        // Fetch messages for a specific chat
        public async Task GetChatMessages(string chatId, int limit = 50)
        {
            var messages = await _messageService.GetMessagesAsync(chatId, limit);
            await Clients.Caller.SendAsync("ReceiveChatMessages", messages);
        }
    }
}
