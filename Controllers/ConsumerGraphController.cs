using firnal.dashboard.data;
using firnal.dashboard.services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace firnal.dashboard.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConsumerGraphController : ControllerBase
    {
        private readonly IConsumerGraphService _consumerGraphService;

        public ConsumerGraphController(IConsumerGraphService consumerGraphService)
        {
            _consumerGraphService = consumerGraphService;
        }

        [HttpPost]
        public async Task<IActionResult> SolomonSearch([FromBody] SolomonSearchRequest filters)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                await _consumerGraphService.GetSearchResults(filters, userEmail);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
