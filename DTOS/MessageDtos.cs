
using System;
using System.Collections.Generic;
using Involved_Chat.Models;

namespace Involved_Chat.DTOS
{
    public class MessageDto
    {
        public string Id { get; set; } = null!;
        public string ChatId { get; set; } = null!;
        public UserDto Sender { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public string Status { get; set; } = "sent";
    }

    public class GetMessagesResponseDto
    {
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public UserDto OtherUser { get; set; } = null!;
    }
}
