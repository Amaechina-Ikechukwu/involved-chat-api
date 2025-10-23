namespace Involved_Chat.DTOS
{
    public class PhotoUpdateDto
    {
        public string PhotoUrl { get; set; } = null!;
    }

    public class AboutUpdateDto
    {
        public string? About { get; set; }
    }

    public class BlockDto
    {
        public string TargetUserId { get; set; } = null!;
    }

    public class PushTokenDto
    {
        public string PushToken { get; set; } = null!;
    }

    public class DisplayNameDto
    {
        public string? DisplayName { get; set; }
    }

    public class LocationUpdateDto
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
