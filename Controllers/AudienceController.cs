using firnal.dashboard.services.Interfaces;
using firnal.dashboard.services.v2.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace firnal.dashboard.api.v2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AudienceController : ControllerBase
    {
        private readonly IAudienceService _audienceService;
        private readonly ISchemaService _schemaService;

        public AudienceController(IAudienceService audienceService, ISchemaService schemaService)
        {
            _audienceService = audienceService;
            _schemaService = schemaService;
        }

        [HttpPost("uploadAudience")]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            var schemas = (await _schemaService.GetSchemaForUserId(userEmail)).First();
            if (string.IsNullOrEmpty(schemas)) schemas = "ADMIN";

            bool success = await _audienceService.UploadAudienceFiles(files, schemas);
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

        [HttpGet("getAudiences")]
        public async Task<IActionResult> GetAudiences()
        {
            var result = await _audienceService.GetAudiences();
            return Ok(result);
        }

        [HttpGet("getAverageIncomeForUpload/{uploadId}")]
        public async Task<IActionResult> GetAverageIncomeForUpload(int uploadId)
        {
            var result = await _audienceService.GetAverageIncomeForUpload(uploadId);
            return Ok(result);
        }

        [HttpGet("getGenderVariance/{uploadId}")]
        public async Task<IActionResult> GetGenderVariance(int uploadId)
        {
            var result = await _audienceService.GetGenderVariance(uploadId);
            return Ok(result);
        }

        [HttpGet("getAgeDistribution/{uploadId}")]
        public async Task<IActionResult> GetAgeDistribution(int uploadId)
        {
            var result = await _audienceService.GetAgeDistribution(uploadId);
            return Ok(result);
        }

        [HttpGet("getAudienceConcentration/{uploadId}")]
        public async Task<IActionResult> GetAudienceConcentration(int uploadId)
        {
            var result = await _audienceService.GetAudienceConcentration(uploadId);
            return Ok(result);
        }

        [HttpGet("getIncomeDistribution/{uploadId}")]
        public async Task<IActionResult> GetIncomeDistribution(int uploadId)
        {
            var result = await _audienceService.GetIncomeDistribution(uploadId);
            return Ok(result);
        }

        [HttpGet("getSampleData/{uploadId}")]
        public async Task<IActionResult> GetSampleData(int uploadId)
        {
            var result = await _audienceService.GetAppendedSampleData(uploadId);
            return Ok(result);
        }

    }
}
