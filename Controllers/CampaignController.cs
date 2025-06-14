using firnal.dashboard.services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace firnal.dashboard.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CampaignController : ControllerBase
    {
        private readonly ICampaignService _campaignService;

        public CampaignController(ICampaignService campaignService)
        {
            _campaignService = campaignService;
        }

        // GET: api/<ValuesController>
        [HttpGet("GetTotalUsersAsync")]
        public async Task<IActionResult> Get(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var count = await _campaignService.GetTotalUsersAsync(schemaName, startDate, endDate);
            return Ok(new { count });
        }

        [HttpGet("GetNewUsers")]
        public async Task<IActionResult> GetNewUsers(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var newUsers = await _campaignService.GetNewUsersAsync(schemaName, startDate, endDate);
            return Ok(newUsers);
        }

        [HttpGet("GetCampaignUserDetails")]
        public async Task<IActionResult> GetCampaignUserDetails(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var campaignUserDetails = await _campaignService.GetCampaignUserDetailsAsync(schemaName, startDate, endDate);
            return Ok(campaignUserDetails);
        }

        [HttpGet("GetZips")]
        public async Task<IActionResult> GetZips(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var zips = await _campaignService.GetDistinctZips(schemaName, startDate, endDate);
            return Ok(zips);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var all = await _campaignService.GetAll(schemaName, startDate, endDate);

            // Return CSV file as a response
            Response.Headers["Content-Disposition"] = "attachment; filename=campaign_data.csv";
            return File(all, "text/csv", "campaign_data.csv");
        }

        [HttpGet("GetAllEnriched")]
        public async Task<IActionResult> GetAllEnriched(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var all = await _campaignService.GetAllEnriched(schemaName, startDate, endDate);

            // Return CSV file as a response
            Response.Headers["Content-Disposition"] = "attachment; filename=campaign_data.csv";
            return File(all, "text/csv", "campaign_data.csv");
        }

        [HttpGet("GetNewUsersOverPast7Days")]
        public async Task<IActionResult> GetNewUsersOverPast7Days(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var users = await _campaignService.GetNewUsersOverPast7Days(schemaName, startDate, endDate);
            return Ok(users);
        }

        [HttpGet("GetGenderDistribution")]
        public async Task<IActionResult> GetGenderDistribution(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var genderDistribution = await _campaignService.GetGenderVariance(schemaName, startDate, endDate);
            return Ok(genderDistribution);
        }

        [HttpGet("GetAverageIncome")]
        public async Task<IActionResult> GetAverageIncome(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var averageIncome = await _campaignService.GetAverageIncome(schemaName, startDate, endDate);
            return Ok(averageIncome);
        }

        [HttpGet("GetAgeRange")]
        public async Task<IActionResult> GetAgeRange(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var ageRange = await _campaignService.GetAgeRange(schemaName, startDate, endDate);
            return Ok(ageRange);
        }

        [HttpGet("GetTopicBreakdown")]
        public async Task<IActionResult> GetTopicBreakdown(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var topicBreakdown = await _campaignService.GetTopicBreakdown(schemaName, startDate, endDate);
            return Ok(topicBreakdown);
        }

        [HttpGet("GetProfessionalBreakdown")]
        public async Task<IActionResult> GetProfessionBreakdown(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var professionBreakdown = await _campaignService.GetProfessionBreakdown(schemaName, startDate, endDate);
            return Ok(professionBreakdown);
        }
    }
}
