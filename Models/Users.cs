using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Involved_Chat.Models
{
    public class User
    {
        [BsonId] // Primary key in Mongo
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("username")]
        public string Username { get; set; } = null!;

        [BsonElement("email")]
        public string Email { get; set; } = null!;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = null!; // 🔒 don’t expose directly

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
