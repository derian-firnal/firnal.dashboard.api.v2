using firnal.dashboard.repositories.v2.Interfaces;
using firnal.dashboard.services.v2.Interfaces;
using Microsoft.AspNetCore.Http;

namespace firnal.dashboard.services.v2
{
    public class AudienceService : IAudienceService
    {
        private readonly IAudienceRepository _audienceRepository;

        public AudienceService(IAudienceRepository audienceRepository)
        {
            _audienceRepository = audienceRepository;
        }

        public async Task<bool> UploadAudienceFiles(List<IFormFile> files)
        {
            return await _audienceRepository.UploadAudienceFiles(files);
        }
    }
}
