using firnal.dashboard.services.v2.Interfaces;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace firnal.dashboard.api.v2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]
    public class AudienceController : ControllerBase
    {
        private readonly IAudienceService _audienceService;

        public AudienceController(IAudienceService audienceService)
        {
            _audienceService = audienceService;
        }

        [HttpPost("uploadAudience")]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files)
        {
            bool success = await _audienceService.UploadAudienceFiles(files);
            return Ok(new { success });
        }

        [HttpGet("getAudienceUploadDetails")]
        public async Task<IActionResult> GetAudienceUploadDetails()
        {
            var details = await _audienceService.GetAudienceUploadDetails();
            return Ok(details);
        }

        [HttpGet("getUploadedFileCount")]
        public async Task<IActionResult> GetUploadedFileCount()
        {
            var result = await _audienceService.GetTotalAudienceUploadFileCount();
            return Ok(result);
        }

        [HttpGet("getUniqueRecordsCount")]
        public async Task<IActionResult> GetUniqueRecordsCount()
        {
            var result = await _audienceService.GetUniqueRecordsCount();
            return Ok(result);
        }
    }
}
