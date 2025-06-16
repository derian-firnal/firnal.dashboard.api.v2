using firnal.dashboard.data.v2;
using Microsoft.AspNetCore.Http;

namespace firnal.dashboard.services.v2.Interfaces
{
    public interface IAudienceService
    {
        Task<bool> UploadAudienceFiles(List<IFormFile> files);
        Task<List<AudienceUploadDetails>> GetAudienceUploadDetails();
    }
}
