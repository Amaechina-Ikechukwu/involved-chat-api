using System;
using System.Collections.Generic;

namespace Involved_Chat.DTOS
{
    public class UserDto
    {
        public string Id { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? PhotoURL { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public string? Status { get; set; }
        public List<string> Contacts { get; set; } = new();
        public List<string> ConnectionIds { get; set; } = new();
        public string? About { get; set; }
        public List<string> BlockedUsers { get; set; } = new();
    }
}
