using firnal.dashboard.data.v2;
using Microsoft.AspNetCore.Http;

namespace firnal.dashboard.repositories.v2.Interfaces
{
    public interface IAudienceRepository
    {
        Task<bool> UploadAudienceFiles(List<IFormFile> files);
        Task<List<AudienceUploadDetails>> GetAudienceUploadDetails();
    }
}
