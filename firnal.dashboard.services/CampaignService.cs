using firnal.dashboard.data;
using firnal.dashboard.repositories.Interfaces;
using firnal.dashboard.services.Interfaces;
using System.Text;

namespace firnal.dashboard.services
{
    public class CampaignService : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepository;

        public CampaignService(ICampaignRepository campaignRepository)
        {
            _campaignRepository = campaignRepository;
        }

        public async Task<List<AgeRange>> GetAgeRange(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetAgeRange(schemaName, startDate, endDate);
        }

        public async Task<byte[]> GetAll(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var result = await _campaignRepository.GetAll(schemaName, startDate, endDate);

            // Convert list to CSV format using StringBuilder
            var csv = new StringBuilder();

            // Get headers dynamically from the Campaign class properties
            var properties = typeof(Campaign).GetProperties();

            // Write headers: wrap each header in quotes
            csv.AppendLine(string.Join(",", properties.Select(p => $"\"{p.Name}\"")));

            // Append data rows: wrap each field in quotes and escape inner quotes
            foreach (var campaign in result)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(campaign, null)?.ToString() ?? "";
                    // Escape any double quotes by replacing " with ""
                    value = value.Replace("\"", "\"\"");
                    return $"\"{value}\"";
                });

                csv.AppendLine(string.Join(",", values));
            }

            // Convert CSV string to byte array using UTF8 encoding
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<byte[]> GetAllEnriched(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            var result = await _campaignRepository.GetAllEnriched(schemaName, startDate, endDate);

            // Convert list to CSV format using StringBuilder
            var csv = new StringBuilder();

            // Get headers dynamically from the Campaign class properties
            var properties = typeof(CampaignEnriched).GetProperties();

            // Write headers: wrap each header in quotes
            csv.AppendLine(string.Join(",", properties.Select(p => $"\"{p.Name}\"")));

            // Append data rows: wrap each field in quotes and escape inner quotes
            foreach (var campaign in result)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(campaign, null)?.ToString() ?? "";
                    // Escape any double quotes by replacing " with ""
                    value = value.Replace("\"", "\"\"");
                    return $"\"{value}\"";
                });

                csv.AppendLine(string.Join(",", values));
            }

            // Convert CSV string to byte array using UTF8 encoding
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<int> GetAverageIncome(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetAverageIncome(schemaName, startDate, endDate);
        }

        public async Task<List<CampaignUserDetails>> GetCampaignUserDetailsAsync(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetCampaignUserDetailsAsync(schemaName, startDate, endDate);
        }

        public async Task<List<Heatmap>> GetDistinctZips(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetDistinctZips(schemaName, startDate, endDate);
        }

        public async Task<GenderVariance> GetGenderVariance(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetGenderVariance(schemaName, startDate, endDate);
        }

        public async Task<int> GetNewUsersAsync(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetNewUsersAsync(schemaName, startDate, endDate);
        }

        public async Task<List<UsageData>> GetNewUsersOverPast7Days(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetNewUsersOverPast7Days(schemaName, startDate, endDate);
        }

        public async Task<List<ProfessionData>> GetProfessionBreakdown(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetProfessionBreakdown(schemaName, startDate, endDate);
        }

        public async Task<List<TopicData>> GetTopicBreakdown(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetTopicBreakdown(schemaName, startDate, endDate);
        }

        public async Task<int> GetTotalUsersAsync(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            return await _campaignRepository.GetTotalUsersAsync(schemaName, startDate, endDate);
        }
    }
}
