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

        // 🔹 User connects to SignalR
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            Console.WriteLine($"[SignalR] Connected user: {userId}, connectionId: {connectionId}");

            if (!string.IsNullOrEmpty(userId))
                await _userService.UpdateStatusAsync(userId, true, null, connectionId);

            await base.OnConnectedAsync();
        }

        // 🔹 User disconnects
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            Console.WriteLine($"[SignalR] Disconnected user: {userId}, connectionId: {connectionId}");

            if (!string.IsNullOrEmpty(userId))
                await _userService.UpdateStatusAsync(userId, false, DateTime.UtcNow, connectionId);

            await base.OnDisconnectedAsync(exception);
        }

        // 🔹 Send a message between users
        public async Task SendMessage(string receiverId, string content)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId))
            {
                Console.WriteLine("[SignalR] SendMessage failed: sender not authenticated");
                return;
            }

            // Get or create a chat
            var chat = await _chatService.GetOrCreateChatAsync(senderId, receiverId);

            // Save message to DB
            var message = await _messageService.SendMessageAsync(chat.Id, senderId, receiverId, content);

            // Update last message preview
            await _chatService.UpdateChatPreviewAsync(chat.Id, senderId, receiverId, content, message.SentAt);

            // Add both users to each other's contacts
            await _userService.AddContactAsync(senderId, receiverId);

            // Send message to receiver (if online)
            await Clients.User(receiverId).SendAsync("ReceiveMessage", new
            {
                messageId = message.Id,
                chatId = chat.Id,
                senderId,
                receiverId,
                content,
                sentAt = message.SentAt
            });

            // Send delivery confirmation to sender
            await Clients.Caller.SendAsync("MessageDelivered", message.Id);

            // 🔹 Send updated chat info to both users
            var senderInfo = await _userService.GetUserInfoAsync(senderId);
            var receiverInfo = await _userService.GetUserInfoAsync(receiverId);

            var chatForSender = await _chatService.GetChatByIdAsync(chat.Id);
            var unreadCountForSender = chatForSender.UserAId == senderId ? chatForSender.UnreadCountA : chatForSender.UnreadCountB;

            await Clients.Caller.SendAsync("ChatUpdated", new
            {
                chatId = chat.Id,
                otherUser = receiverInfo,
                lastMessage = content,
                lastMessageTime = message.SentAt,
                lastMessageSenderId = senderId,
                unreadCount = unreadCountForSender
            });

            var chatForReceiver = await _chatService.GetChatByIdAsync(chat.Id);
            var unreadCountForReceiver = chatForReceiver.UserAId == receiverId ? chatForReceiver.UnreadCountA : chatForReceiver.UnreadCountB;

            await Clients.User(receiverId).SendAsync("ChatUpdated", new
            {
                chatId = chat.Id,
                otherUser = senderInfo,
                lastMessage = content,
                lastMessageTime = message.SentAt,
                lastMessageSenderId = senderId,
                unreadCount = unreadCountForReceiver
            });

            // Notify receiver of updated total unread count
            var totalUnread = await _chatService.GetTotalUnreadCountAsync(receiverId);
            await Clients.User(receiverId).SendAsync("ReceiveUnreadMessagesCount", totalUnread);
        }

        // 🔹 Mark all messages in chat as read
        public async Task MarkAsRead(string chatId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            await _messageService.MarkAsReadAsync(chatId, userId);
            await Clients.Caller.SendAsync("MessagesRead", chatId);

            // Notify user of updated total unread count
            var totalUnread = await _chatService.GetTotalUnreadCountAsync(userId);
            await Clients.Caller.SendAsync("ReceiveUnreadMessagesCount", totalUnread);
        }

        // 🔹 Fetch all messages for a chat
        public async Task GetChatMessages(string chatId, int limit = 50)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("ChatsError", "User not authenticated");
                return;
            }

            try
            {
                var messages = await _messageService.GetMessagesAsync(chatId, limit);
                Console.WriteLine($"[SignalR] Sending {messages.Count} messages for chat {chatId}");
                await Clients.Caller.SendAsync("ReceiveChatMessages", messages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error in GetChatMessages: {ex.Message}");
                await Clients.Caller.SendAsync("ChatsError", $"Error fetching messages: {ex.Message}");
            }
        }

        // 🔹 Fetch list of all user chats (for inbox view)
        public async Task GetIndividualChats()
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
                var enriched = new List<object>();

                foreach (var chat in chats)
                {
                    var otherUserId = chat.UserAId == userId ? chat.UserBId : chat.UserAId;
                    var otherUser = await _userService.GetUserInfoAsync(otherUserId);
                    var unreadCount = chat.UserAId == userId ? chat.UnreadCountA : chat.UnreadCountB;

                    enriched.Add(new
                    {
                        chatId = chat.Id,
                        otherUser,
                        chat.LastMessage,
                        chat.LastMessageTime,
                        chat.LastMessageSenderId,
                        unreadCount
                    });
                }

                await Clients.Caller.SendAsync("ReceiveChats", enriched);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ChatsError", $"Error fetching chats: {ex.Message}");
            }
        }

        // 🔹 Get total unread messages count
        public async Task GetUnreadMessagesCount()
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("ChatsError", "User not authenticated");
                return;
            }

            try
            {
                var unreadCount = await _chatService.GetTotalUnreadCountAsync(userId);
                await Clients.Caller.SendAsync("ReceiveUnreadMessagesCount", unreadCount);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ChatsError", $"Error fetching unread messages count: {ex.Message}");
            }
        }
    }
}
