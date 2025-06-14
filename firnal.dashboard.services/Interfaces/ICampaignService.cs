using firnal.dashboard.data;

namespace firnal.dashboard.services.Interfaces
{
    public interface ICampaignService
    {
        Task<int> GetTotalUsersAsync(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<CampaignUserDetails>> GetCampaignUserDetailsAsync(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<Heatmap>> GetDistinctZips(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<byte[]> GetAll(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<byte[]> GetAllEnriched(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<int> GetNewUsersAsync(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<UsageData>> GetNewUsersOverPast7Days(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<GenderVariance> GetGenderVariance(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<int> GetAverageIncome(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<AgeRange>> GetAgeRange(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<TopicData>> GetTopicBreakdown(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<ProfessionData>> GetProfessionBreakdown(string schemaName, DateTime? startDate, DateTime? endDate);
    }
}
