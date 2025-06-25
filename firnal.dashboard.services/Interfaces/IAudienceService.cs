using firnal.dashboard.data;
using firnal.dashboard.data.v2;
using Microsoft.AspNetCore.Http;

namespace firnal.dashboard.services.v2.Interfaces
{
    public interface IAudienceService
    {
        Task<int> GetTotalAudienceUploadFileCount();
        Task<int> GetUniqueRecordsCount();
        Task<bool> UploadAudienceFiles(List<IFormFile> files, string userSchema);
        Task<List<AudienceUploadDetails>> GetAudienceUploadDetails();
        Task<List<Audience>> GetAudiences();
        Task<decimal> GetAverageIncomeForUpload(int uploadId);
        Task<List<GenderBreakdown>> GetGenderVariance(int uploadId);
        Task<List<AgeGroupStat>> GetAgeDistribution(int uploadFileId);
        Task<List<AudienceConcentrationStat>> GetAudienceConcentration(int uploadFileId);
        Task<List<IncomeGroupStat>> GetIncomeDistribution(int uploadFileId);
        Task<List<AppendedSampleRow>> GetAppendedSampleData(int uploadFileId);
        Task<int> EnrichAudience(int uploadId);
    }
}
