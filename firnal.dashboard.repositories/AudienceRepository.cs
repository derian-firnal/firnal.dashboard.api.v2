using firnal.dashboard.data;
using firnal.dashboard.repositories.v2.Interfaces;
using Microsoft.AspNetCore.Http;
using Snowflake.Data.Client;

namespace firnal.dashboard.repositories.v2
{
    public class AudienceRepository : IAudienceRepository
    {
        private readonly SnowflakeDbConnectionFactory _dbFactory;

        public AudienceRepository(SnowflakeDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<bool> UploadAudienceFiles(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentException("No files provided.");

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file.FileName);
                var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(file.FileName));
                int rowCount = 0;
                string status = "Success";
                string errorMessage = null;

                try
                {
                    // Save file to temp path
                    await using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    using var conn = _dbFactory.GetConnection();
                    conn.Open();

                    // Upload to Snowflake stage
                    using (var putCmd = conn.CreateCommand())
                    {
                        putCmd.CommandText = $"PUT file://{tempFilePath} @dashboard_internal_stage OVERWRITE = TRUE;";
                        putCmd.ExecuteNonQuery();
                    }

                    // Load into AudienceUploads
                    using (var copyCmd = conn.CreateCommand())
                    {
                        copyCmd.CommandText = $@"
                    COPY INTO AudienceUploads
                    FROM @dashboard_internal_stage/{fileName}
                    FILE_FORMAT = (FORMAT_NAME = 'audience_csv_format')
                    ON_ERROR = 'CONTINUE'
                    PURGE = TRUE;
                ";

                        using var reader = copyCmd.ExecuteReader();
                        while (reader.Read())
                        {
                            rowCount += reader.GetInt32(1); // Get rows loaded from COPY INTO result
                        }
                    }
                }
                catch (Exception ex)
                {
                    status = "Failed";
                    errorMessage = ex.Message;
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);

                    // Write audit record
                    try
                    {
                        using var conn = _dbFactory.GetConnection();
                        conn.Open();

                        using var auditCmd = conn.CreateCommand();
                        auditCmd.CommandText = $@"
                    INSERT INTO AudienceUploadAudit (FileName, RowCount, Status, ErrorMessage)
                    VALUES (@fileName, @rowCount, @status, @errorMsg)";
                        auditCmd.Parameters.Add(new SnowflakeDbParameter
                        {
                            ParameterName = "fileName",
                            DbType = System.Data.DbType.String,
                            Value = fileName
                        });
                        auditCmd.Parameters.Add(new SnowflakeDbParameter
                        {
                            ParameterName = "rowCount",
                            DbType = System.Data.DbType.Int32,
                            Value = rowCount
                        });
                        auditCmd.Parameters.Add(new SnowflakeDbParameter
                        {
                            ParameterName = "status",
                            DbType = System.Data.DbType.String,
                            Value = status
                        });
                        auditCmd.Parameters.Add(new SnowflakeDbParameter
                        {
                            ParameterName = "errorMsg",
                            DbType = System.Data.DbType.String,
                            Value = errorMessage ?? ""
                        });

                        auditCmd.ExecuteNonQuery();
                    }
                    catch (Exception logEx)
                    {
                        Console.Error.WriteLine($"❌ Failed to write audit log: {logEx.Message}");
                    }
                }
            }

            return true;
        }
    }
}
