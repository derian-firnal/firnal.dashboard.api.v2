using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using firnal.dashboard.data;
using firnal.dashboard.data.v2;
using firnal.dashboard.repositories.v2.Interfaces;
using Microsoft.AspNetCore.Http;
using Snowflake.Data.Client;
using System;
using System.Data;
using System.Globalization;

namespace firnal.dashboard.repositories.v2
{
    public class AudienceRepository : IAudienceRepository
    {
        private readonly SnowflakeDbConnectionFactory _dbFactory;
        private readonly string _dbName = "DASHBOARD_V2";
        private readonly string _schemaName = "PUBLIC";

        public AudienceRepository(SnowflakeDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<AudienceUploadDetails>> GetAudienceUploadDetails()
        {
            var results = new List<AudienceUploadDetails>();

            using var conn = _dbFactory.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                SELECT ID, FileName, RowCount, UploadedAt, Status
                FROM {_dbName}.{_schemaName}.AudienceUploadFiles
                ORDER BY UploadedAt DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new AudienceUploadDetails
                {
                    Id = reader.GetInt64(0),
                    AudienceName = reader.GetString(1),
                    Records = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    UploadedAt = reader.GetDateTime(3),
                    Status = reader.GetString(4),
                    MatchRate = $"{new Random().Next(1, 100)}%"
                });
            }

            return results;
        }

        public async Task<bool> UploadAudienceFiles(List<IFormFile> files)
        {
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file.FileName);
                string status = "Success";
                string errorMessage = null;
                int rowCount = 0;
                long uploadFileId = -1;

                try
                {
                    // Step 1: Insert file metadata
                    using var metaConn = _dbFactory.GetConnection();
                    metaConn.Open();

                    using var insertMeta = metaConn.CreateCommand();
                    insertMeta.CommandText = $@"
                INSERT INTO {_dbName}.{_schemaName}.AudienceUploadFiles 
                (FileName, RowCount, Status, ErrorMessage)
                VALUES (:fileName, 0, 'InProgress', '')";

                    insertMeta.Parameters.Add(new SnowflakeDbParameter
                    {
                        ParameterName = "fileName",
                        DbType = DbType.String,
                        Value = fileName
                    });

                    insertMeta.ExecuteNonQuery();

                    using var getIdCmd = metaConn.CreateCommand();
                    getIdCmd.CommandText = $@"
                SELECT ID FROM {_dbName}.{_schemaName}.AudienceUploadFiles 
                WHERE FileName = :fileName 
                ORDER BY UploadedAt DESC 
                LIMIT 1";

                    getIdCmd.Parameters.Add(new SnowflakeDbParameter
                    {
                        ParameterName = "fileName",
                        DbType = DbType.String,
                        Value = fileName
                    });

                    using (var reader = getIdCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            uploadFileId = reader.GetInt64(0);
                            Console.WriteLine($"📁 UploadFileId: {uploadFileId} for {fileName}");
                        }
                        else
                        {
                            throw new Exception("❌ Failed to retrieve UploadFileId after metadata insert.");
                        }
                    }

                    // Step 2: Parse CSV
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
                        Console.WriteLine($"📦 {fileName} → Parsed {records.Count} records");
                    }

                    using var conn = _dbFactory.GetConnection();
                    conn.Open();

                    const int batchSize = 100;

                    for (int i = 0; i < records.Count; i += batchSize)
                    {
                        var batch = records.Skip(i).Take(batchSize).ToList();
                        using var transaction = conn.BeginTransaction();

                        try
                        {
                            using var cmd = conn.CreateCommand();
                            cmd.Transaction = transaction;

                            var props = typeof(AudienceUploadRecord).GetProperties();
                            var columns = props.Select(p => p.Name).ToList();
                            columns.Add("UPLOADFILE_ID");

                            var allRows = new List<string>();
                            int rowIndex = 0;

                            foreach (var record in batch)
                            {
                                var valuePlaceholders = new List<string>();

                                foreach (var prop in props)
                                {
                                    string paramName = $"{prop.Name}_{rowIndex}";
                                    var val = prop.GetValue(record) ?? DBNull.Value;

                                    cmd.Parameters.Add(new SnowflakeDbParameter
                                    {
                                        ParameterName = paramName,
                                        DbType = DbType.String,
                                        Value = val
                                    });

                                    valuePlaceholders.Add($":{paramName}");
                                }

                                string fileParam = $"UPLOADFILE_ID_{rowIndex}";
                                cmd.Parameters.Add(new SnowflakeDbParameter
                                {
                                    ParameterName = fileParam,
                                    DbType = DbType.Int64,
                                    Value = uploadFileId
                                });
                                valuePlaceholders.Add($":{fileParam}");

                                allRows.Add($"({string.Join(",", valuePlaceholders)})");
                                rowIndex++;
                            }

                            cmd.CommandText = $@"
                        INSERT INTO {_dbName}.{_schemaName}.AudienceUploads 
                        ({string.Join(",", columns)})
                        VALUES {string.Join(",", allRows)}";

                            cmd.ExecuteNonQuery();
                            transaction.Commit();

                            rowCount += batch.Count;
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"❌ Insert error in file {fileName}: {ex.Message}");
                            errorMessage = ex.Message;
                            status = "Failed";
                            transaction.Rollback();
                            break;
                        }
                    }

                    Console.WriteLine($"✅ Inserted {rowCount} rows from {fileName}");

                    // Step 3: Finalize file metadata
                    using var updateMeta = metaConn.CreateCommand();
                    updateMeta.CommandText = $@"
                UPDATE {_dbName}.{_schemaName}.AudienceUploadFiles
                SET RowCount = :rowCount, Status = :status, ErrorMessage = :errorMsg
                WHERE ID = :id";

                    updateMeta.Parameters.Add(new SnowflakeDbParameter { ParameterName = "rowCount", DbType = DbType.Int32, Value = rowCount });
                    updateMeta.Parameters.Add(new SnowflakeDbParameter { ParameterName = "status", DbType = DbType.String, Value = status });
                    updateMeta.Parameters.Add(new SnowflakeDbParameter { ParameterName = "errorMsg", DbType = DbType.String, Value = errorMessage ?? "" });
                    updateMeta.Parameters.Add(new SnowflakeDbParameter { ParameterName = "id", DbType = DbType.Int64, Value = uploadFileId });

                    updateMeta.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"❌ Failed to fully process {fileName}: {ex.Message}");
                }
            }

            return true;
        }

        public async Task<int> GetTotalAudienceUploadFileCount()
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $"SELECT COUNT(*) FROM {_dbName}.{_schemaName}.AudienceUploadFiles";

            var result = await conn.QuerySingleAsync<int>(sql);
            return result;
        }

        public async Task<int> GetUniqueRecordsCount()
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            //var sql = $"SELECT COUNT(DISTINCT ID) FROM {_dbName}.{_schemaName}.AudienceUploads";
            var sql = $"SELECT COUNT(*) FROM {_dbName}.{_schemaName}.AudienceUploads";
            var result = await conn.QuerySingleAsync<int>(sql);
            return result;
        }

        public async Task<List<Audience>> GetAudiences()
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $"SELECT * FROM {_dbName}.{_schemaName}.AudienceUploadFiles";

            var result = await conn.QueryAsync<Audience>(sql);
            return result.ToList();
        }

        public async Task<decimal> GetAverageIncomeForUpload(int uploadId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT
                    ROUND(AVG(
                        CASE
                            WHEN INCOME_RANGE ILIKE 'Less than $20,000' THEN 15000
                            WHEN INCOME_RANGE ILIKE '$20,000 to $44,999' THEN 32500
                            WHEN INCOME_RANGE ILIKE '$45,000 to $59,999' THEN 52500
                            WHEN INCOME_RANGE ILIKE '$60,000 to $74,999' THEN 67500
                            WHEN INCOME_RANGE ILIKE '$75,000 to $99,999' THEN 87500
                            WHEN INCOME_RANGE ILIKE '$100,000 to $149,999' THEN 125000
                            WHEN INCOME_RANGE ILIKE '$150,000 to $199,999' THEN 175000
                            WHEN INCOME_RANGE ILIKE '$200,000 to $249,000' THEN 225000
                            WHEN INCOME_RANGE ILIKE '$250,000%' THEN 275000
                            ELSE NULL
                        END
                    ), 0) AS AVERAGE_INCOME
                FROM {_dbName}.{_schemaName}.AUDIENCEUPLOADS
                WHERE INCOME_RANGE IS NOT NULL AND UPLOADFILE_ID = :uploadId;";

            var result = await conn.ExecuteScalarAsync<decimal>(sql, new { uploadId = uploadId });
            return result;
        }

        public async Task<List<GenderBreakdown>> GetGenderVariance(int uploadFileId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT 
                    GENDER,
                    COUNT(*) AS Count,
                    ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 2) AS Percent
                FROM {_dbName}.{_schemaName}.AUDIENCEUPLOADS
                WHERE GENDER IS NOT NULL
                  AND UPLOADFILE_ID = :uploadId
                GROUP BY GENDER;";

            var result = await conn.QueryAsync<GenderBreakdown>(sql, new { uploadId = uploadFileId });

            return result.ToList();
        }

        public async Task<List<AgeGroupStat>> GetAgeDistribution(int uploadFileId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT 
                    AGE_RANGE AS AgeRange,
                    COUNT(*) AS Count
                FROM {_dbName}.{_schemaName}.AUDIENCEUPLOADS
                WHERE AGE_RANGE IS NOT NULL
                  AND UPLOADFILE_ID = :uploadId
                GROUP BY AGE_RANGE
                ORDER BY 
                  CASE 
                    WHEN AGE_RANGE = '25-34' THEN 1
                    WHEN AGE_RANGE = '35-44' THEN 2
                    WHEN AGE_RANGE = '45-54' THEN 3
                    WHEN AGE_RANGE = '55-64' THEN 4
                    WHEN AGE_RANGE = '65 and older' THEN 5
                    ELSE 99
                  END;";

            var result = await conn.QueryAsync<AgeGroupStat>(sql, new { uploadId = uploadFileId });

            return result.ToList();
        }

        public async Task<List<AudienceConcentrationStat>> GetAudienceConcentration(int uploadFileId)
        {

            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT 
                    PERSONAL_STATE AS Location,
                    COUNT(*) AS Count
                FROM {_dbName}.{_schemaName}.AUDIENCEUPLOADS
                WHERE PERSONAL_STATE IS NOT NULL
                  AND UPLOADFILE_ID = :uploadId
                GROUP BY PERSONAL_STATE
                ORDER BY Count DESC
                LIMIT 10;
            ";

            var result = await conn.QueryAsync<AudienceConcentrationStat>(sql, new { uploadId = uploadFileId });
            return result.ToList();
        }

        public async Task<List<IncomeGroupStat>> GetIncomeDistribution(int uploadFileId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT 
                    INCOME_RANGE AS IncomeRange,
                    COUNT(*) AS Count
                FROM {_dbName}.{_schemaName}.AUDIENCEUPLOADS
                WHERE INCOME_RANGE IS NOT NULL
                  AND UPLOADFILE_ID = :uploadId
                GROUP BY INCOME_RANGE
                ORDER BY 
                  CASE
                    WHEN INCOME_RANGE = 'Less than $20,000' THEN 1
                    WHEN INCOME_RANGE = '$20,000 to $44,999' THEN 2
                    WHEN INCOME_RANGE = '$45,000 to $59,999' THEN 3
                    WHEN INCOME_RANGE = '$60,000 to $74,999' THEN 4
                    WHEN INCOME_RANGE = '$75,000 to $99,999' THEN 5
                    WHEN INCOME_RANGE = '$100,000 to $149,999' THEN 6
                    WHEN INCOME_RANGE = '$150,000 to $199,999' THEN 7
                    WHEN INCOME_RANGE = '$200,000 to $249,000' THEN 8
                    WHEN INCOME_RANGE ILIKE '$250,000%' THEN 9
                    ELSE 99
                  END;
            ";

            var result = await conn.QueryAsync<IncomeGroupStat>(sql, new { uploadId = uploadFileId });
            return result.ToList();
        }
        public async Task<List<AppendedSampleRow>> GetAppendedSampleData(int uploadFileId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT 
                    FIRST_NAME AS FirstName,
                    LAST_NAME AS LastName,
                    PERSONAL_EMAILS AS Email,
                    GENDER,
                    AGE_RANGE AS AgeRange,
                    INCOME_RANGE AS IncomeRange,
                    PERSONAL_STATE AS State
                FROM {_dbName}.{_schemaName}.AUDIENCEUPLOADS
                WHERE UPLOADFILE_ID = :uploadId
                LIMIT 10;
            ";

            var result = await conn.QueryAsync<AppendedSampleRow>(sql, new { uploadId = uploadFileId });
            return result.ToList();
        }
    }
}
