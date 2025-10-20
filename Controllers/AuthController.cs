using Microsoft.AspNetCore.Mvc;
using Involved_Chat.Services;
using Involved_Chat.AuthDtos;
using Google.Cloud.SecretManager.V1;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Involved_Chat.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly string _jwtKey;

        public AuthController(AuthService authService)
        {
            _authService = authService;

            // Retrieve JWT key from Google Secret Manager safely once in constructor
            _jwtKey = GetSecret("jwt_key");
        }

        // --- Helper Method ---
        private static string GetSecret(string secretId)
        {
            var client = SecretManagerServiceClient.Create();
            var secretName = new SecretVersionName("tangle2", secretId, "latest");
            var result = client.AccessSecretVersion(secretName);
            return result.Payload.Data.ToStringUtf8();
        }

        // --- REGISTER ---
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CustomRegisterRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Request body cannot be null" });

                var user = await _authService.RegisterAsync(request.Username, request.Email, request.Password);

                // Generate token using AuthService (which can use _jwtKey internally)
                var token = await _authService.LoginAsync(request.Email, request.Password);

                if (user == null)
                    return StatusCode(500, new { message = "User registration failed", success = false });

                var data = new
                {
                    token,
                    id = user.Id,
                    username = user.Username,
                    email = user.Email
                };

                return Ok(new { message = "User registered successfully", data, success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // --- LOGIN ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginAuthRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Request body cannot be null" });

                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                    return BadRequest(new { message = "Email and password are required" });

                var token = await _authService.LoginAsync(request.Email, request.Password);

                if (token == null)
                    return Unauthorized(new { error = "Invalid credentials" });

                var user = await _authService.GetUserByEmailAsync(request.Email);

                var data = new
                {
                    token,
                    id = user?.Id,
                    username = user?.Username,
                    email = user?.Email
                };

                return Ok(new { message = "Login successful", data, success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
