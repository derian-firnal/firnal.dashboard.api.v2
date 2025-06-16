using firnal.dashboard.data.v2;
using Microsoft.AspNetCore.Http;

namespace firnal.dashboard.repositories.v2.Interfaces
{
    public interface IAudienceRepository
    {
        Task<int> GetTotalAudienceUploadFileCount();
        Task<int> GetUniqueRecordsCount();
        Task<bool> UploadAudienceFiles(List<IFormFile> files);
        Task<List<AudienceUploadDetails>> GetAudienceUploadDetails();
    }
}
