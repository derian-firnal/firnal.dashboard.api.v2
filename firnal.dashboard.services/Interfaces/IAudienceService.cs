using firnal.dashboard.data.v2;
using Microsoft.AspNetCore.Http;

namespace firnal.dashboard.services.v2.Interfaces
{
    public interface IAudienceService
    {
        Task<int> GetTotalAudienceUploadFileCount();
        Task<int> GetUniqueRecordsCount();
        Task<bool> UploadAudienceFiles(List<IFormFile> files);
        Task<List<AudienceUploadDetails>> GetAudienceUploadDetails();
        Task<List<Audience>> GetAudiences();
        Task<decimal> GetAverageIncomeForUpload(int uploadId);
    }
}
