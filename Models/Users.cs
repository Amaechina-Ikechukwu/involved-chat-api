using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Involved_Chat.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("username")]
        public string Username { get; set; } = null!;

        [BsonElement("email")]
        public string Email { get; set; } = null!;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = null!;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

      

        [BsonElement("displayName")]
        public string DisplayName { get; set; } = null!; // full name to show in chat list

        [BsonElement("photoURL")]
        public string? PhotoURL { get; set; } // avatar / profile pic

        [BsonElement("isOnline")]
        public bool IsOnline { get; set; } = false; // for presence tracking

        [BsonElement("lastSeen")]
        public DateTime? LastSeen { get; set; } // last active timestamp

        [BsonElement("status")]
        public string? Status { get; set; } // e.g. "Available", "Busy", "At work"

        [BsonElement("contacts")]
        public List<string> Contacts { get; set; } = new(); // list of user IDs

        [BsonElement("connectionIds")]
        public List<string> ConnectionIds { get; set; } = new(); // active SignalR connection IDs (if multi-device)

    [BsonElement("pushTokens")]
    public List<string> PushTokens { get; set; } = new(); // Expo push tokens or other push tokens

    [BsonElement("location")]
    public UserLocation? Location { get; set; }

        [BsonElement("about")]
        public string? About { get; set; } // short bio

        [BsonElement("blockedUsers")]
        public List<string> BlockedUsers { get; set; } = new(); // optional: for privacy control
    }

    public class UserLocation
    {
        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }
    }
}
