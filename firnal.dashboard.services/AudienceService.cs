using CsvHelper.Configuration;
using CsvHelper;
using firnal.dashboard.data;
using firnal.dashboard.data.v2;
using firnal.dashboard.repositories.v2.Interfaces;
using firnal.dashboard.services.v2.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Globalization;

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
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file.FileName);
                string status = "Success";
                string errorMessage = null;
                int rowCount = 0;

                long uploadFileId = await _audienceRepository.InsertUploadMetadataAsync(fileName, userSchema);

                try
                {
                    List<AudienceUploadRecord> records;
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HeaderValidated = null,
                        MissingFieldFound = null,
                        TrimOptions = TrimOptions.Trim,
                    }))
                    {
                        records = csv.GetRecords<AudienceUploadRecord>().ToList();
                    }

                    var result = await _audienceRepository.InsertAudienceRecordsAsync(records, uploadFileId);
                    rowCount = result.rowCount;
                    errorMessage = result.errorMessage;
                    status = result.errorMessage == null ? "Completed" : "Failed";
                }
                catch (Exception ex)
                {
                    status = "Failed";
                    errorMessage = ex.Message;
                }

                await _audienceRepository.FinalizeUploadAsync(uploadFileId, rowCount, status, errorMessage);
            }

            return true;
        }


        public async Task<List<AppendedSampleRow>> GetAppendedSampleData(int uploadId)
        {
            return await _audienceRepository.GetAppendedSampleData(uploadId);
        }

        public async Task<int> EnrichAudience(int uploadId)
        {
            var records = await _audienceRepository.GetAudienceUploadRecordsByUploadId(uploadId);
            if (!records.Any()) return 0;

            var results = await _audienceRepository.EnrichAudience(uploadId, records);

            if (results > 0)
                await _audienceRepository.MarkUploadAsEnriched(uploadId);

            return results;
        }
    }
}
