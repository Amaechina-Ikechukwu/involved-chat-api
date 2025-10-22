using Involved_Chat.Models;
using Involved_Chat.DTOS;
using Involved_Chat.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Google.Cloud.SecretManager.V1;


namespace Involved_Chat.Controllers
{
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly MessageService _messageService;
        private readonly Cloudinary _cloudinary;

        string GetSecret(string secretId)
        {
            var client = SecretManagerServiceClient.Create();
            var secretName = new SecretVersionName("tangle2", secretId, "latest");
            AccessSecretVersionResponse result = client.AccessSecretVersion(secretName);
            return result.Payload.Data.ToStringUtf8();
        }
        [ActivatorUtilitiesConstructor]
        public UserController(UserService userService, MessageService messageService, IConfiguration config)
        {
            _userService = userService;
            _messageService = messageService;

            var cloudName = GetSecret("cloundinary_name");
            var apiKey = GetSecret("cloudinary_api_key");
            var apiSecret = GetSecret("cloudinary_api_secret");

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }
       


        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            var user = await _userService.GetUserInfoAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // GET /api/user/me - returns the user info for the token owner
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            // Debug: Log all claims to help troubleshoot
            var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
          
            
            // Jwt contains NameIdentifier claim with the user id (see AuthService.LoginAsync)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value; // Fallback to standard JWT 'sub' claim
            
        
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            var user = await _userService.GetUserInfoAsync(userId);
            if (user == null) return NotFound(new { message = "User not found", success = false });
            
            return Ok(new { message = "User info retrieved", data = user, success = true });
        }

        [HttpPut("{id}/photo")]
        public async Task<IActionResult> UpdatePhoto(string id, [FromForm] IFormFile file)
        {
               if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "uploads"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                await _userService.UpdatePhotoUrlAsync(id, uploadResult.SecureUrl.ToString());
                return Ok(new { url = uploadResult.SecureUrl.ToString(), message = "Photo upload successful", success = true });
            }
            else
            {
                return StatusCode((int)uploadResult.StatusCode, uploadResult.Error?.Message ?? "Upload failed");
            }
    
        }

        [HttpPut("{id}/about")]
        public async Task<IActionResult> UpdateAbout(string id, [FromBody] AboutUpdateDto dto)
        {
            if (dto == null) return BadRequest();
            await _userService.UpdateAboutAsync(id, dto.About ?? string.Empty);
            return Ok(new{message="About updated",success=true});
        }

        [HttpPut("{id}/displayname")]
        public async Task<IActionResult> UpdateDisplayName(string id, [FromBody] DisplayNameDto dto)
        {
            if (dto == null) return BadRequest();
            await _userService.UpdateDisplayNameAsync(id, dto.DisplayName ?? string.Empty);
            return Ok(new { message = "Display name updated", success = true });
        }

        [HttpPost("{id}/block")]
        public async Task<IActionResult> BlockUser(string id, [FromBody] BlockDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TargetUserId)) return BadRequest("targetUserId is required");
            await _userService.BlockUserAsync(id, dto.TargetUserId);
            return Ok(new{message="This user has been blocked",success=true});
        }

        [HttpPost("{id}/unblock")]
        public async Task<IActionResult> UnblockUser(string id, [FromBody] BlockDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TargetUserId)) return BadRequest("targetUserId is required");
            await _userService.UnblockUserAsync(id, dto.TargetUserId);
            return Ok(new{message="This user has been unblocked",success=true});
        }

        [HttpGet("{id}/contacts")]
        public async Task<IActionResult> GetContacts(string id)
        {
            var contacts = await _userService.GetContactsAsync(id);
            return Ok(contacts);
        }

        [HttpPost("{id}/push-token")]
        public async Task<IActionResult> AddPushToken(string id, [FromBody] PushTokenDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PushToken)) return BadRequest("pushToken is required");
            await _userService.AddPushTokenAsync(id, dto.PushToken);
            return Ok(new { message = "Push token added", success = true });
        }

        [HttpDelete("{id}/push-token")]
        public async Task<IActionResult> RemovePushToken(string id, [FromBody] PushTokenDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PushToken)) return BadRequest("pushToken is required");
            await _userService.RemovePushTokenAsync(id, dto.PushToken);
            return Ok(new { message = "Push token removed", success = true });
        }

        [HttpPut("{id}/location")]
        public async Task<IActionResult> UpdateLocation(string id, [FromBody] LocationUpdateDto dto)
        {
            if (dto == null) return BadRequest();
            await _userService.UpdateLocationAsync(id, dto.Latitude, dto.Longitude);
            return Ok(new { message = "Location updated", success = true });
        }

        [HttpGet("nearby")]
        public async Task<ActionResult<PaginatedUserResponse>> GetNearbyUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _userService.GetNearbyUsersAsync(userId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("debug/locations")]
        public async Task<IActionResult> DebugLocations()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var currentUser = await _userService.GetUserInfoAsync(userId);
            var allUsersCount = await _userService.GetAllUsersWithLocationCountAsync();
            
            return Ok(new 
            { 
                currentUser = new 
                {
                    id = currentUser?.Id,
                    username = currentUser?.Username,
                    hasLocation = currentUser?.Location != null,
                    location = currentUser?.Location
                },
                totalUsersWithLocation = allUsersCount,
                message = "Debug info for nearby users feature"
            });
        }
    }

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
