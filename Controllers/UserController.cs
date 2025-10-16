using Involved_Chat.Models;
using Involved_Chat.DTOS;
using Involved_Chat.Services;
using Microsoft.AspNetCore.Mvc;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.SecretManager.V1;


namespace Involved_Chat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
}
