using System;
using System.Collections.Generic;
using Involved_Chat.DTOS;

namespace Involved_Chat.DTOS
{
    // Minimal user snippet for chat peer
    public class ChatPeerDto
    {
        public string Id { get; set; } = null!;
        public string DisplayName { get; set; } = string.Empty;
        public string? PhotoURL { get; set; }
    }

    // Chat list item with nested other user details
    public class ChatListItemDto
    {
        public string ChatId { get; set; } = null!;
        // Return full user profile for the other participant
        public UserDto OtherUser { get; set; } = new UserDto
        {
            Id = string.Empty,
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
        public string? LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public string? LastMessageSenderId { get; set; }
        public int UnreadCount { get; set; }
    }
}
