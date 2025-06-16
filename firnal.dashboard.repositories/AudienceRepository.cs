using CsvHelper;
using CsvHelper.Configuration;
using firnal.dashboard.data;
using firnal.dashboard.data.v2;
using firnal.dashboard.repositories.v2.Interfaces;
using Microsoft.AspNetCore.Http;
using Snowflake.Data.Client;
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
                    MatchRate = "92%" // placeholder
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

    }
}
