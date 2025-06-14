using firnal.dashboard.data;

namespace firnal.dashboard.repositories.Interfaces
{
    public interface ICampaignRepository
    {
        Task<int> GetTotalUsersAsync(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<CampaignUserDetails>> GetCampaignUserDetailsAsync(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<Heatmap>> GetDistinctZips(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<Campaign>> GetAll(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<CampaignEnriched>> GetAllEnriched(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<int> GetNewUsersAsync(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<UsageData>> GetNewUsersOverPast7Days(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<GenderVariance> GetGenderVariance(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<int> GetAverageIncome(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<AgeRange>> GetAgeRange(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<TopicData>> GetTopicBreakdown(string schemaName, DateTime? startDate, DateTime? endDate);
        Task<List<ProfessionData>> GetProfessionBreakdown(string schemaName, DateTime? startDate, DateTime? endDate);
    }
}
