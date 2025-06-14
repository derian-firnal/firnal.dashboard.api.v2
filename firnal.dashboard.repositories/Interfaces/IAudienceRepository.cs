using Microsoft.AspNetCore.Http;

namespace firnal.dashboard.repositories.v2.Interfaces
{
    public interface IAudienceRepository
    {
        bool UploadAudienceFile(IFormFile file);
    }
}
