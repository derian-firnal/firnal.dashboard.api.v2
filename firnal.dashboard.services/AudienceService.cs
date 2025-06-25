using firnal.dashboard.data;
using firnal.dashboard.data.v2;
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

        public async Task<List<AgeGroupStat>> GetAgeDistribution(int uploadFileId)
        {
            return await _audienceRepository.GetAgeDistribution(uploadFileId);
        }

        public async Task<List<AudienceConcentrationStat>> GetAudienceConcentration(int uploadFileId)
        {
            return await _audienceRepository.GetAudienceConcentration(uploadFileId);
        }

        public async Task<List<IncomeGroupStat>> GetIncomeDistribution(int uploadId)
        {
            return await _audienceRepository.GetIncomeDistribution(uploadId);
        }

        public async Task<List<Audience>> GetAudiences()
        {
            return await _audienceRepository.GetAudiences();
        }

        public async Task<List<AudienceUploadDetails>> GetAudienceUploadDetails()
        {
            return await _audienceRepository.GetAudienceUploadDetails();
        }

        public async Task<decimal> GetAverageIncomeForUpload(int uploadId)
        {
            return await _audienceRepository.GetAverageIncomeForUpload(uploadId);
        }

        public async Task<List<GenderBreakdown>> GetGenderVariance(int uploadId)
        {
            return await _audienceRepository.GetGenderVariance(uploadId);
        }

        public async Task<int> GetTotalAudienceUploadFileCount()
        {
            return await _audienceRepository.GetTotalAudienceUploadFileCount();
        }

        public async Task<int> GetUniqueRecordsCount()
        {
            return await _audienceRepository.GetUniqueRecordsCount();
        }

        public async Task<bool> UploadAudienceFiles(List<IFormFile> files, string userSchema)
        {
            return await _audienceRepository.UploadAudienceFiles(files, userSchema);
        }

        public async Task<List<AppendedSampleRow>> GetAppendedSampleData(int uploadId)
        {
            return await _audienceRepository.GetAppendedSampleData(uploadId);
        }
    }
}
