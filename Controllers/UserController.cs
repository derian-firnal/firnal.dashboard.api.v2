using firnal.dashboard.data;
using firnal.dashboard.services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firnal.dashboard.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsers();
            return Ok(users);
        }

        // POST: api/User/SaveUser
        [HttpPost("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] User updatedUser)
        {
            try
            {
                var result = await _userService.UpdateUser(updatedUser);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception as needed.
                return StatusCode(500, "An error occurred while updating the user.");
            }
        }

        [HttpPost("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromBody] string userId)
        {
            try
            {
                var result = await _userService.DeleteUser(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception as needed.
                return StatusCode(500, "An error occurred while deleting the user.");
            }
        }
    }
}
