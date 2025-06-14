using firnal.dashboard.data;
using firnal.dashboard.repositories.v2.Interfaces;
using Microsoft.AspNetCore.Http;

namespace firnal.dashboard.repositories.v2
{
    public class AudienceRepository : IAudienceRepository
    {
        private readonly SnowflakeDbConnectionFactory _dbFactory;

        public AudienceRepository(SnowflakeDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public bool UploadAudienceFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty.");

            // Save file temporarily
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(file.FileName));
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            try
            {
                using var conn = _dbFactory.GetConnection();
                conn.Open();

                // 1. PUT file to internal stage
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"PUT file://{tempFilePath} @dashboard_internal_stage OVERWRITE = TRUE;";
                    cmd.ExecuteNonQuery();
                }

                // 2. COPY INTO table
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        COPY INTO AudienceUploads
                        FROM @dashboard_internal_stage
                        FILE_FORMAT = (FORMAT_NAME = 'my_csv_format')
                        ON_ERROR = 'CONTINUE';";
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Audience Upload Failed!");
                return false;
            }
            finally
            {
                // Clean up the temp file
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }
    }
}
