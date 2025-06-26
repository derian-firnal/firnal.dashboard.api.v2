using Dapper;
using firnal.dashboard.data;
using firnal.dashboard.data.v2;
using firnal.dashboard.repositories.v2.Interfaces;
using Snowflake.Data.Client;
using System.Data;

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
            try
            {
                using var conn = _dbFactory.GetConnection();
                conn.Open();

                var sql = $@"
                    SELECT ID, FileName as AudienceName, RowCount as Records, UploadedAt, Status, IsEnriched
                    FROM {_dbName}.{_schemaName}.AudienceUploadFiles
                    ORDER BY UploadedAt DESC";

                var results = await conn.QueryAsync<AudienceUploadDetails>(sql);
                return results.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public async Task<List<AudienceUploadDetails>> GetAudienceUploadDetailsForLoggedInUser(string companyName)
        {
            try
            {
                using var conn = _dbFactory.GetConnection();
                conn.Open();

                var sql = $@"
                    SELECT ID, FileName as AudienceName, RowCount as Records, UploadedAt, Status, IsEnriched
                    FROM {_dbName}.{_schemaName}.AudienceUploadFiles
                    WHERE USER_SCHEMA = :companyName
                    ORDER BY UploadedAt DESC";

                var results = await conn.QueryAsync<AudienceUploadDetails>(sql, new { companyName });
                return results.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public async Task<AudienceUploadDetails> GetAudienceUploadDetailsById(string uploadFileId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT ID, FileName as AudienceName, RowCount as Records, UploadedAt, Status, IsEnriched
                FROM {_dbName}.{_schemaName}.AudienceUploadFiles
                WHERE ID = :uploadFileId";

            var results = await conn.QuerySingleAsync<AudienceUploadDetails>(sql, new { uploadFileId });
            return results;
        }


        #region -- file upload methods --

        public async Task<long> InsertUploadMetadataAsync(string fileName, string userSchema)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO {_dbName}.{_schemaName}.AudienceUploadFiles 
                (FileName, RowCount, Status, ErrorMessage, User_Schema)
                VALUES (:fileName, 0, 'Processing', '', :userSchema)";

            cmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = "fileName", DbType = DbType.String, Value = fileName });
            cmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = "userSchema", DbType = DbType.String, Value = userSchema });
            cmd.ExecuteNonQuery();

            using var getIdCmd = conn.CreateCommand();
            getIdCmd.CommandText = $@"
                SELECT ID FROM {_dbName}.{_schemaName}.AudienceUploadFiles 
                WHERE FileName = :fileName ORDER BY UploadedAt DESC LIMIT 1";
            getIdCmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = "fileName", DbType = DbType.String, Value = fileName });

            using var reader = getIdCmd.ExecuteReader();
            return reader.Read() ? reader.GetInt64(0) : throw new Exception("❌ Failed to retrieve upload ID.");
        }

        public async Task<(int rowCount, string errorMessage)> InsertAudienceRecordsAsync(List<AudienceUploadRecord> records, long uploadFileId)
        {
            var columnNames = new List<string>
            {
                "ID",
                "UPLOADFILE_ID",
                "ADDITIONAL_PERSONAL_EMAILS",
                "AGE_RANGE",
                "BITO_IDS",
                "BUSINESS_EMAIL",
                "BUSINESS_EMAIL_LAST_SEEN",
                "BUSINESS_EMAIL_VALIDATION_STATUS",
                "CATEGORY",
                "CC_ID",
                "CHILDREN",
                "COMPANY_ADDRESS",
                "COMPANY_CITY",
                "COMPANY_DESCRIPTION",
                "COMPANY_DOMAIN",
                "COMPANY_EMPLOYEE_COUNT",
                "COMPANY_LAST_UPDATED",
                "COMPANY_LINKEDIN_URL",
                "COMPANY_NAME",
                "COMPANY_PHONE",
                "COMPANY_REVENUE",
                "COMPANY_SIC",
                "COMPANY_STATE",
                "COMPANY_ZIP",
                "DEPARTMENT",
                "DPV_CODE",
                "EDUCATION_HISTORY",
                "FIRST_NAME",
                "GENDER",
                "HOMEOWNER",
                "INCOME_RANGE",
                "JOB_TITLE",
                "JOB_TITLE_LAST_UPDATED",
                "LAST_NAME",
                "LAST_UPDATED",
                "LINKEDIN_URL",
                "MAIDS",
                "MARRIED",
                "MOBILE_PHONE",
                "NET_WORTH",
                "PERSONAL_ADDRESS",
                "PERSONAL_CITY",
                "PERSONAL_COUNTRY",
                "PERSONAL_EMAILS",
                "PERSONAL_EMAILS_LAST_SEEN",
                "PERSONAL_EMAILS_VALIDATION_STATUS",
                "PERSONAL_STATE",
                "PERSONAL_ZIP",
                "PERSONAL_ZIP4",
                "PRIMARY_INDUSTRY",
                "PROFESSIONAL_ADDRESS",
                "PROFESSIONAL_ADDRESS_2",
                "PROFESSIONAL_CITY",
                "PROFESSIONAL_STATE",
                "PROFESSIONAL_ZIP",
                "PROFESSIONAL_ZIP4",
                "RELATED_DOMAINS",
                "SENIORITY_LEVEL",
                "SOCIAL_CONNECTIONS",
                "SUB_CATEGORY",
                "TIMES_SEEN",
                "TOPIC",
                "WORK_HISTORY",
                "SCORE",
                "SHA256_LC_HEM",
                "SOURCEFILE"
            };

            int rowCount = 0;
            string errorMessage = null;

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
                    //var columns = props.Select(p => p.Name).ToList(); //.Append("UPLOADFILE_ID").ToList();

                    var allRows = new List<string>();
                    int rowIndex = 0;

                    var propsDict = typeof(AudienceUploadRecord).GetProperties().ToDictionary(p => p.Name, p => p);

                    var orderedProps = columnNames
                        .Select(col => col == "UPLOADFILE_ID"
                            ? null // Will add manually
                            : propsDict.TryGetValue(col, out var prop) ? prop : null)
                        .ToList();

                    foreach (var record in batch)
                    {
                        var valuePlaceholders = new List<string>();

                        for (int j = 0; j < orderedProps.Count; j++)
                        {
                            var prop = orderedProps[j];
                            if (columnNames[j] == "UPLOADFILE_ID")
                            {
                                var paramName = $"UPLOADFILE_ID_{rowIndex}";
                                cmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = paramName, DbType = DbType.Int64, Value = uploadFileId });
                                valuePlaceholders.Add($":{paramName}");
                            }
                            else if (prop != null)
                            {
                                var paramName = $"{prop.Name}_{rowIndex}";
                                var val = prop.GetValue(record) ?? DBNull.Value;
                                cmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = paramName, DbType = DbType.String, Value = val });
                                valuePlaceholders.Add($":{paramName}");
                            }
                            else
                            {
                                valuePlaceholders.Add("NULL"); // Placeholder for missing property
                            }
                        }

                        allRows.Add($"({string.Join(",", valuePlaceholders)})");
                        rowIndex++;
                    }

                    cmd.CommandText = $@"
                        INSERT INTO {_dbName}.{_schemaName}.AudienceUploads 
                        ({string.Join(",", columnNames)})
                        VALUES {string.Join(",", allRows)}";

                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    rowCount += batch.Count;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    errorMessage = ex.Message;
                    break;
                }
            }

            return (rowCount, errorMessage);
        }

        public async Task FinalizeUploadAsync(long uploadFileId, int rowCount, string status, string errorMessage)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = $@"
                UPDATE {_dbName}.{_schemaName}.AudienceUploadFiles
                SET RowCount = :rowCount, Status = :status, ErrorMessage = :errorMsg
                WHERE ID = :id";

            cmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = "rowCount", DbType = DbType.Int32, Value = rowCount });
            cmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = "status", DbType = DbType.String, Value = status });
            cmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = "errorMsg", DbType = DbType.String, Value = errorMessage ?? "" });
            cmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = "id", DbType = DbType.Int64, Value = uploadFileId });

            cmd.ExecuteNonQuery();
        }

        //public async Task<bool> UploadAudienceFiles(List<IFormFile> files, string userSchema)
        //{
        //    foreach (var file in files)
        //    {
        //        string fileName = Path.GetFileName(file.FileName);
        //        string status = "Success";
        //        string errorMessage = null;
        //        int rowCount = 0;
        //        long uploadFileId = -1;

        //        try
        //        {
        //            // Step 1: Insert file metadata
        //            using var metaConn = _dbFactory.GetConnection();
        //            metaConn.Open();

        //            using var insertMeta = metaConn.CreateCommand();
        //            insertMeta.CommandText = $@"
        //                INSERT INTO {_dbName}.{_schemaName}.AudienceUploadFiles 
        //                (FileName, RowCount, Status, ErrorMessage, User_Schema)
        //                VALUES (:fileName, 0, 'InProgress', '', :userSchema)";

        //            insertMeta.Parameters.Add(new SnowflakeDbParameter
        //            {
        //                ParameterName = "fileName",
        //                DbType = DbType.String,
        //                Value = fileName
        //            });

        //            insertMeta.Parameters.Add(new SnowflakeDbParameter
        //            {
        //                ParameterName = "userSchema",
        //                DbType = DbType.String,
        //                Value = userSchema
        //            });

        //            insertMeta.ExecuteNonQuery();

        //            using var getIdCmd = metaConn.CreateCommand();
        //            getIdCmd.CommandText = $@"
        //                SELECT ID FROM {_dbName}.{_schemaName}.AudienceUploadFiles 
        //                WHERE FileName = :fileName 
        //                ORDER BY UploadedAt DESC 
        //                LIMIT 1";

        //            getIdCmd.Parameters.Add(new SnowflakeDbParameter
        //            {
        //                ParameterName = "fileName",
        //                DbType = DbType.String,
        //                Value = fileName
        //            });

        //            using (var reader = getIdCmd.ExecuteReader())
        //            {
        //                if (reader.Read())
        //                {
        //                    uploadFileId = reader.GetInt64(0);
        //                    Console.WriteLine($"📁 UploadFileId: {uploadFileId} for {fileName}");
        //                }
        //                else
        //                {
        //                    throw new Exception("❌ Failed to retrieve UploadFileId after metadata insert.");
        //                }
        //            }

        //            // Step 2: Parse CSV
        //            List<AudienceUploadRecord> records;
        //            using (var reader = new StreamReader(file.OpenReadStream()))
        //            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        //            {
        //                HeaderValidated = null,
        //                MissingFieldFound = null,
        //                TrimOptions = TrimOptions.Trim,
        //            }))
        //            {
        //                records = csv.GetRecords<AudienceUploadRecord>().ToList();
        //                Console.WriteLine($"📦 {fileName} → Parsed {records.Count} records");
        //            }

        //            using var conn = _dbFactory.GetConnection();
        //            conn.Open();

        //            const int batchSize = 100;

        //            for (int i = 0; i < records.Count; i += batchSize)
        //            {
        //                var batch = records.Skip(i).Take(batchSize).ToList();
        //                using var transaction = conn.BeginTransaction();

        //                try
        //                {
        //                    using var cmd = conn.CreateCommand();
        //                    cmd.Transaction = transaction;

        //                    var props = typeof(AudienceUploadRecord).GetProperties();
        //                    var columns = props.Select(p => p.Name).ToList();
        //                    columns.Add("UPLOADFILE_ID");

        //                    var allRows = new List<string>();
        //                    int rowIndex = 0;

        //                    foreach (var record in batch)
        //                    {
        //                        var valuePlaceholders = new List<string>();

        //                        foreach (var prop in props)
        //                        {
        //                            string paramName = $"{prop.Name}_{rowIndex}";
        //                            var val = prop.GetValue(record) ?? DBNull.Value;

        //                            cmd.Parameters.Add(new SnowflakeDbParameter
        //                            {
        //                                ParameterName = paramName,
        //                                DbType = DbType.String,
        //                                Value = val
        //                            });

        //                            valuePlaceholders.Add($":{paramName}");
        //                        }

        //                        string fileParam = $"UPLOADFILE_ID_{rowIndex}";
        //                        cmd.Parameters.Add(new SnowflakeDbParameter
        //                        {
        //                            ParameterName = fileParam,
        //                            DbType = DbType.Int64,
        //                            Value = uploadFileId
        //                        });
        //                        valuePlaceholders.Add($":{fileParam}");

        //                        allRows.Add($"({string.Join(",", valuePlaceholders)})");
        //                        rowIndex++;
        //                    }

        //                    cmd.CommandText = $@"
        //                INSERT INTO {_dbName}.{_schemaName}.AudienceUploads 
        //                ({string.Join(",", columns)})
        //                VALUES {string.Join(",", allRows)}";

        //                    cmd.ExecuteNonQuery();
        //                    transaction.Commit();

        //                    rowCount += batch.Count;
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.Error.WriteLine($"❌ Insert error in file {fileName}: {ex.Message}");
        //                    errorMessage = ex.Message;
        //                    status = "Failed";
        //                    transaction.Rollback();
        //                    break;
        //                }
        //            }

        //            Console.WriteLine($"✅ Inserted {rowCount} rows from {fileName}");

        //            // Step 3: Finalize file metadata
        //            using var updateMeta = metaConn.CreateCommand();
        //            updateMeta.CommandText = $@"
        //        UPDATE {_dbName}.{_schemaName}.AudienceUploadFiles
        //        SET RowCount = :rowCount, Status = :status, ErrorMessage = :errorMsg
        //        WHERE ID = :id";

        //            updateMeta.Parameters.Add(new SnowflakeDbParameter { ParameterName = "rowCount", DbType = DbType.Int32, Value = rowCount });
        //            updateMeta.Parameters.Add(new SnowflakeDbParameter { ParameterName = "status", DbType = DbType.String, Value = status });
        //            updateMeta.Parameters.Add(new SnowflakeDbParameter { ParameterName = "errorMsg", DbType = DbType.String, Value = errorMessage ?? "" });
        //            updateMeta.Parameters.Add(new SnowflakeDbParameter { ParameterName = "id", DbType = DbType.Int64, Value = uploadFileId });

        //            updateMeta.ExecuteNonQuery();
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.Error.WriteLine($"❌ Failed to fully process {fileName}: {ex.Message}");
        //        }
        //    }

        //    return true;
        //}


        #endregion


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
                ORDER BY RANDOM()
                LIMIT 10;
            ";

            var result = await conn.QueryAsync<AppendedSampleRow>(sql, new { uploadId = uploadFileId });
            return result.ToList();
        }

        public async Task<List<AudienceUploadRecord>> GetAudienceUploadRecordsByUploadId(int uploadFileId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT 
                    * 
                from {_dbName}.{_schemaName}.AUDIENCEUPLOADS
                WHERE UPLOADFILE_ID = :uploadId";

            var result = await conn.QueryAsync<AudienceUploadRecord>(sql, new { uploadId = uploadFileId });
            return result.ToList();
        }

        public async Task<int> EnrichAudience(int uploadFileId, List<AudienceUploadRecord> records)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            // 1. Create temporary table
            var tempTableName = "TEMP_AUDIENCE_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var createTempSql = $@"CREATE TEMPORARY TABLE {_dbName}.{_schemaName}.{tempTableName} LIKE {_dbName}.{_schemaName}.AudienceUploads;";
            using (var cmd = conn.CreateCommand()) { cmd.CommandText = createTempSql; cmd.ExecuteNonQuery(); }

            // 2. Insert records into temp table
            const int batchSize = 100;
            var props = typeof(AudienceUploadRecord).GetProperties();
            var columns = props.Select(p => p.Name).ToList();

            for (int i = 0; i < records.Count; i += batchSize)
            {
                var batch = records.Skip(i).Take(batchSize).ToList();
                using var transaction = conn.BeginTransaction();
                using var cmd = conn.CreateCommand();
                cmd.Transaction = transaction;

                var allRows = new List<string>();
                int rowIndex = 0;

                foreach (var record in batch)
                {
                    var valuePlaceholders = new List<string>();
                    foreach (var prop in props)
                    {
                        string paramName = $"{prop.Name}_{rowIndex}";
                        object value = prop.GetValue(record) ?? DBNull.Value;
                        cmd.Parameters.Add(new SnowflakeDbParameter { ParameterName = paramName, DbType = DbType.String, Value = value });
                        valuePlaceholders.Add($":{paramName}");
                    }

                    allRows.Add($"({string.Join(",", valuePlaceholders)})");
                    rowIndex++;
                }

                cmd.CommandText = $@"
                    INSERT INTO {_dbName}.{_schemaName}.{tempTableName} ({string.Join(",", columns)})
                    VALUES {string.Join(",", allRows)}";
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }

            // 3. Get enrichment scores
            var couponScores = await GetScoreColumn(conn, "GET_COUPON_CONVERSION_SCORE", $"{_dbName}.{_schemaName}.{tempTableName}");
            var emailScores = await GetScoreColumn(conn, "GET_EMAIL_ENGAGEMENT_SCORE", $"{_dbName}.{_schemaName}.{tempTableName}");
            var financialScores = await GetScoreColumn(conn, "GET_FINANCIAL_OVEREXTENTION_SCORE", $"{_dbName}.{_schemaName}.{tempTableName}");
            var impulseScores = await GetScoreColumn(conn, "GET_IMPULSE_BUY_SCORE", $"{_dbName}.{_schemaName}.{tempTableName}");
            var influencerScores = await GetScoreColumn(conn, "GET_INFLUENCER_RESPONSE_SCORE", $"{_dbName}.{_schemaName}.{tempTableName}");
            var smsScores = await GetScoreColumn(conn, "GET_SMS_ENGAGEMENT_SCORE", $"{_dbName}.{_schemaName}.{tempTableName}");
            var subscriptionScores = await GetScoreColumn(conn, "GET_SUBSCRIPTION_PURCHASE_SCORE", $"{_dbName}.{_schemaName}.{tempTableName}");

            // 4. Merge records with enrichment scores
            var enrichedList = records.Select((record, i) =>
            {
                var enriched = new AudienceUploadRecordEnriched();

                foreach (var prop in typeof(AudienceUploadRecord).GetProperties())
                {
                    var value = prop.GetValue(record);
                    typeof(AudienceUploadRecordEnriched).GetProperty(prop.Name)?.SetValue(enriched, value);
                }

                enriched.ENRICHMENT_COUPONSCORE = couponScores.ElementAtOrDefault(i);
                enriched.ENRICHMENT_EMAILSCORE = emailScores.ElementAtOrDefault(i);
                enriched.ENRICHMENT_FINANCIALSCORE = financialScores.ElementAtOrDefault(i);
                enriched.ENRICHMENT_IMPULSESCORE = impulseScores.ElementAtOrDefault(i);
                //enriched.InfluencerScores = influencerScores.ElementAtOrDefault(i);
                enriched.ENRICHMENT_SMSSCORE = smsScores.ElementAtOrDefault(i);
                enriched.ENRICHMENT_SUBSCRIPTIONSCORE = subscriptionScores.ElementAtOrDefault(i);
                enriched.UPLOADFILE_ID = uploadFileId.ToString();

                return enriched;
            }).ToList();

            // 5. Insert enriched records
            var enrichedProps = typeof(AudienceUploadRecordEnriched).GetProperties().ToList();
            var enrichedColumns = enrichedProps.Select(p => p.Name).ToList();

            for (int i = 0; i < enrichedList.Count; i += batchSize)
            {
                var batch = enrichedList.Skip(i).Take(batchSize).ToList();
                using var transaction = conn.BeginTransaction();
                using var cmd = conn.CreateCommand();
                cmd.Transaction = transaction;

                var allRows = new List<string>();
                int rowIndex = 0;

                foreach (var record in batch)
                {
                    var valuePlaceholders = new List<string>();
                    foreach (var prop in enrichedProps)
                    {
                        var paramName = $"{prop.Name}_{rowIndex}";
                        var val = prop.GetValue(record) ?? DBNull.Value;
                        cmd.Parameters.Add(new SnowflakeDbParameter
                        {
                            ParameterName = paramName,
                            DbType = DbType.String, // adjust types as needed
                            Value = val
                        });
                        valuePlaceholders.Add($":{paramName}");
                    }
                    allRows.Add($"({string.Join(",", valuePlaceholders)})");
                    rowIndex++;
                }

                cmd.CommandText = $@"
                    INSERT INTO {_dbName}.{_schemaName}.AudienceUploads_Enriched
                    ({string.Join(",", enrichedColumns)})
                    VALUES {string.Join(",", allRows)}";

                cmd.ExecuteNonQuery();

                transaction.Commit();
            }

            return records.Count;
        }


        private async Task<List<double>> GetScoreColumn(IDbConnection conn, string procedureName, string tempAudienceTable)
        {
            try
            {
                string sql = $"CALL ENRICHMENTS.PUBLIC.{procedureName}(:tempAudienceTable)";
                var parameters = new DynamicParameters();
                parameters.Add("tempAudienceTable", tempAudienceTable);

                var result = await conn.QueryAsync<double>(sql, parameters);
                return result.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task MarkUploadAsEnriched(int uploadId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            var sql = $@"
                UPDATE {_dbName}.{_schemaName}.AudienceUploadFiles
                SET IsEnriched = TRUE
                WHERE ID = :uploadId";

            await conn.ExecuteAsync(sql, new { uploadId = uploadId });
        }

        public async Task<List<AudienceUploadRecord>> GetAudiencesByUploadId(string uploadFileId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT *
                FROM {_dbName}.{_schemaName}.AudienceUploads
                WHERE UPLOADFILE_ID = :uploadFileId";

            var results = await conn.QueryAsync<AudienceUploadRecord>(sql, new { uploadFileId });
            return results.ToList();
        }

        public async Task<List<AudienceUploadRecordEnriched>> GetEnrichedAudiencesByUploadId(string uploadFileId)
        {
            using var conn = _dbFactory.GetConnection();
            conn.Open();

            var sql = $@"
                SELECT *
                FROM {_dbName}.{_schemaName}.AudienceUploads_Enriched
                WHERE UPLOADFILE_ID = :uploadFileId";

            var results = await conn.QueryAsync<AudienceUploadRecordEnriched>(sql, new { uploadFileId });
            return results.ToList();
        }
    }
}
