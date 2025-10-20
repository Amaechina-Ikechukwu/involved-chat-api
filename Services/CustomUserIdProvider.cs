using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Involved_Chat.Services
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Try to get the user ID from the NameIdentifier claim (ClaimTypes.NameIdentifier)
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? connection.User?.FindFirst("sub")?.Value; // Fallback to standard JWT 'sub' claim
        }
    }
}
