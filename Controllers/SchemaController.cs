using firnal.dashboard.services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace firnal.dashboard.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SchemaController : ControllerBase
    {
        private readonly ISchemaService _schemaService;

        public SchemaController(ISchemaService schemaService)
        {
            _schemaService = schemaService;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
           var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            var schemas = await _schemaService.GetSchemaForUserId(userEmail);
            return Ok(schemas);
        }
    }
}
