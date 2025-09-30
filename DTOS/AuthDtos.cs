namespace Involved_Chat.AuthDtos
{
    public class CustomRegisterRequest
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class LoginAuthRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
}
