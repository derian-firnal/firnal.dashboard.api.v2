using firnal.dashboard.data;
using firnal.dashboard.services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firnal.dashboard.api.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Username) ||
                string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Role))
            {
                return BadRequest("All fields are required.");
            }

            try
            {
                var userId = await _authService.RegisterUser(request.Email, request.Username, request.Password, request.Role, request.Schemas);

                if (string.IsNullOrEmpty(userId))
                {
                    return StatusCode(500, "User registration failed.");
                }

                return Ok(
                new { 
                    UserId = userId, 
                    Message = "User registered successfully." 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred.", Error = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            try
            {
                var user = await _authService.AuthenticateUser(request.Email, request.Password);

                if (user == null)
                    return Unauthorized("Invalid credentials.");

                return Ok(new { User = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred during login.", Error = ex.Message });
            }
        }
    }
}
