using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Involved_Chat.Models
{
    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)] // MongoDB ObjectId
        public string Id { get; set; } = null!;

        [BsonElement("fromUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string FromUserId { get; set; } = null!;

        [BsonElement("toUserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ToUserId { get; set; } = null!;

        [BsonElement("content")]
        public string Content { get; set; } = null!;

        [BsonElement("sentAt")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isRead")]
        public bool IsRead { get; set; } = false;
    }
}
