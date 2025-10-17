using Microsoft.AspNetCore.Mvc;
using Involved_Chat.Services;
using Microsoft.AspNetCore.Identity.Data;
using Involved_Chat.AuthDtos;
namespace Involved_Chat.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
      [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
   
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        
        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CustomRegisterRequest request)
        {
            try
            {
                var user = await _authService.RegisterAsync(request.Username, request.Email, request.Password);

                // Generate token after successful registration
                var token = await _authService.LoginAsync(request.Email, request.Password);

                var data = new {
                    token,
                    id = user.Id,
                    username = user.Username,
                    email = user.Email
                };

                return Ok(new { message = "User registered successfully", data,success=true });
            }
            catch (Exception ex){
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginAuthRequest request)
        {
            try
            {
                var token = await _authService.LoginAsync(request.Email, request.Password);
                if (token == null)
                {
                    return Unauthorized(new { error = "Invalid Credential" });
                }

                // fetch user to include details in response
                var user = await _authService.GetUserByEmailAsync(request.Email);

                var data = new {
                    token,
                    id = user?.Id,
                    username = user?.Username,
                    email = user?.Email
                };

                return Ok(new { message = "Login successful", data,success=true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
 
}