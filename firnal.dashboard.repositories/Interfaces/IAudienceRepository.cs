using firnal.dashboard.data;
using firnal.dashboard.data.v2;
using Microsoft.AspNetCore.Http;

namespace firnal.dashboard.repositories.v2.Interfaces
{
    public interface IAudienceRepository
    {
        Task<int> GetTotalAudienceUploadFileCount();
        Task<int> GetUniqueRecordsCount();


        // -- upload methods
        // Task<bool> UploadAudienceFiles(List<IFormFile> files, string userSchema);
        Task<long> InsertUploadMetadataAsync(string fileName, string userSchema);
        Task<(int rowCount, string errorMessage)> InsertAudienceRecordsAsync(List<AudienceUploadRecord> records, long uploadFileId);
        Task FinalizeUploadAsync(long uploadFileId, int rowCount, string status, string errorMessage);
        // -- end upload methods


        Task<List<AudienceUploadDetails>> GetAudienceUploadDetails();
        Task<List<Audience>> GetAudiences();
        Task<decimal> GetAverageIncomeForUpload(int uploadId);
        Task<List<GenderBreakdown>> GetGenderVariance(int uploadId);
        Task<List<AgeGroupStat>> GetAgeDistribution(int uploadFileId);
        Task<List<AudienceConcentrationStat>> GetAudienceConcentration(int uploadFileId);
        Task<List<IncomeGroupStat>> GetIncomeDistribution(int uploadFileId);
        Task<List<AppendedSampleRow>> GetAppendedSampleData(int uploadFileId);
    }
}
