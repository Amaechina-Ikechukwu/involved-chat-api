using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Involved_Chat.Models
{
    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("chatId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ChatId { get; set; } = null!; // 🔗 Links to a Chat collection

        [BsonElement("senderId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SenderId { get; set; } = null!;

        [BsonElement("receiverId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ReceiverId { get; set; } = null!;

        [BsonElement("content")]
        public string Content { get; set; } = null!;

        [BsonElement("sentAt")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isRead")]
        public bool IsRead { get; set; } = false;

        [BsonElement("readAt")]
        [BsonIgnoreIfNull]
        public DateTime? ReadAt { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "sent"; 
        // could be: "sent", "delivered", "read", "failed"

        [BsonElement("type")]
        public string Type { get; set; } = "text"; 
        // "text", "image", "video", "audio", "file", etc.

        [BsonElement("replyToMessageId")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ReplyToMessageId { get; set; } 

        [BsonElement("attachments")]
        [BsonIgnoreIfNull]
        public List<string>? Attachments { get; set; } // list of URLs (S3, Firebase, etc.)
    }
}
