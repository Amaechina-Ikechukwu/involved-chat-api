using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Involved_Chat.Models
{
    public class Chat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;
public string ChatKey { get; set; } = null!; // ✅ store "userA_userB" here
        [BsonElement("userAId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserAId { get; set; } = null!;

        [BsonElement("userBId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserBId { get; set; } = null!;

        [BsonElement("lastMessage")]
        [BsonIgnoreIfNull]
        public string? LastMessage { get; set; }

        [BsonElement("lastMessageTime")]
        public DateTime LastMessageTime { get; set; } = DateTime.UtcNow;

        [BsonElement("lastMessageSenderId")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? LastMessageSenderId { get; set; }

        [BsonElement("unreadCountA")]
        public int UnreadCountA { get; set; } = 0;

        [BsonElement("unreadCountB")]
        public int UnreadCountB { get; set; } = 0;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
