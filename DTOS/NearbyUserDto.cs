namespace Involved_Chat.DTOS
{
    public class NearbyUserDto
    {
        public required string Id { get; set; }
        public required string Username { get; set; }
        public required string DisplayName { get; set; }
        public string? PhotoURL { get; set; }
        public double Distance { get; set; } // in meters
    }
}