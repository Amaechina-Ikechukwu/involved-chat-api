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

        [HttpPut("photo")]
        public async Task<IActionResult> UpdatePhoto([FromForm] IFormFile file)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

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
                await _userService.UpdatePhotoUrlAsync(userId, uploadResult.SecureUrl.ToString());
                return Ok(new { url = uploadResult.SecureUrl.ToString(), message = "Photo upload successful", success = true });
            }
            else
            {
                return StatusCode((int)uploadResult.StatusCode, uploadResult.Error?.Message ?? "Upload failed");
            }
    
        }

        [HttpPut("about")]
        public async Task<IActionResult> UpdateAbout([FromBody] AboutUpdateDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (dto == null) return BadRequest();
            await _userService.UpdateAboutAsync(userId, dto.About ?? string.Empty);
            return Ok(new{message="About updated",success=true});
        }

        [HttpPut("displayname")]
        public async Task<IActionResult> UpdateDisplayName([FromBody] DisplayNameDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (dto == null) return BadRequest();
            await _userService.UpdateDisplayNameAsync(userId, dto.DisplayName ?? string.Empty);
            return Ok(new { message = "Display name updated", success = true });
        }

        [HttpPost("block")]
        public async Task<IActionResult> BlockUser([FromBody] BlockDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (dto == null || string.IsNullOrWhiteSpace(dto.TargetUserId)) return BadRequest("targetUserId is required");
            await _userService.BlockUserAsync(userId, dto.TargetUserId);
            return Ok(new{message="This user has been blocked",success=true});
        }

        [HttpPost("unblock")]
        public async Task<IActionResult> UnblockUser([FromBody] BlockDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (dto == null || string.IsNullOrWhiteSpace(dto.TargetUserId)) return BadRequest("targetUserId is required");
            await _userService.UnblockUserAsync(userId, dto.TargetUserId);
            return Ok(new{message="This user has been unblocked",success=true});
        }

        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            var contacts = await _userService.GetContactsAsync(userId);
            return Ok(contacts);
        }

        [HttpPost("push-token")]
        public async Task<IActionResult> AddPushToken([FromBody] PushTokenDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (dto == null || string.IsNullOrWhiteSpace(dto.PushToken)) return BadRequest("pushToken is required");
            await _userService.AddPushTokenAsync(userId, dto.PushToken);
            return Ok(new { message = "Push token added", success = true });
        }

        [HttpDelete("push-token")]
        public async Task<IActionResult> RemovePushToken([FromBody] PushTokenDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (dto == null || string.IsNullOrWhiteSpace(dto.PushToken)) return BadRequest("pushToken is required");
            await _userService.RemovePushTokenAsync(userId, dto.PushToken);
            return Ok(new { message = "Push token removed", success = true });
        }

        [HttpPut("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userId)) 
                return Unauthorized(new { message = "User ID not found in token", success = false });

            if (dto == null) return BadRequest();
            await _userService.UpdateLocationAsync(userId, dto.Latitude, dto.Longitude);
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
}
