using Dapper;
using firnal.dashboard.data;
using firnal.dashboard.repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Data;

namespace firnal.dashboard.repositories
{
    public class CampaignRepository : ICampaignRepository
    {
        private readonly SnowflakeDbConnectionFactory _dbFactory;
        private readonly IMemoryCache _cache;
        private const string DbName = "OUTREACHGENIUS_DRIPS";

        public CampaignRepository(SnowflakeDbConnectionFactory dbFactory, IMemoryCache cache)
        {
            _dbFactory = dbFactory;
            _cache = cache;
        }

        private MemoryCacheEntryOptions GetCacheOptionsForMidnight()
        {
            // Set cache expiration at the next midnight UTC.
            var midnightUtc = DateTimeOffset.UtcNow.Date.AddDays(1);
            return new MemoryCacheEntryOptions { AbsoluteExpiration = midnightUtc };
        }

        public async Task<List<CampaignUserDetails>> GetCampaignUserDetailsAsync(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            string cacheKey = $"CampaignUserDetails_{schemaName}";
            //if (_cache.TryGetValue(cacheKey, out List<CampaignUserDetails>? cachedDetails) && cachedDetails != null)
            //{
            //    return cachedDetails;
            //}

            using var conn = _dbFactory.GetConnection();

            var filters = new List<string>();

            var parameters = new DynamicParameters();

            if (startDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) >= :StartDate");
                parameters.Add("StartDate", startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) <= :EndDate");
                parameters.Add("EndDate", endDate.Value.Date);
            }
            var whereClause = "WHERE " + string.Join(" AND ", filters);

            var sql = $"SELECT first_name, last_name, mobile_phone as personal_phone, gender, age_range, income_range, net_worth FROM {DbName}.{schemaName}.campaign {whereClause}";
            var result = await conn.QueryAsync<CampaignUserDetails>(sql, parameters);
            var details = result.ToList();

            _cache.Set(cacheKey, details, GetCacheOptionsForMidnight());
            return details;
        }

        public async Task<List<Heatmap>> GetDistinctZips(string schemaName, DateTime? startDate, DateTime? endDate) 
        {
            string cacheKey = $"DistinctZips_{schemaName}";
            //if (_cache.TryGetValue(cacheKey, out List<Heatmap>? cachedZips) && cachedZips != null)
            //{
            //    return cachedZips;
            //}

            var filters = new List<string>
            {
                "c.personal_zip IS NOT NULL",
                "c.personal_zip != ''"
            };

            var parameters = new DynamicParameters();

            if (startDate.HasValue)
            {
                filters.Add("TO_DATE(c.created_at) >= :StartDate");
                parameters.Add("StartDate", startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                filters.Add("TO_DATE(c.created_at) <= :EndDate");
                parameters.Add("EndDate", endDate.Value.Date);
            }
            var whereClause = "WHERE " + string.Join(" AND ", filters);

            using var conn = _dbFactory.GetConnection();
            var sql = @$"SELECT 
                            TRY_CAST(c.personal_zip AS INTEGER) AS personal_zip, 
                            z.latitude, 
                            z.longitude, 
                            count(*) as zip_count 
                        FROM {DbName}.{schemaName}.campaign c
                        INNER JOIN OUTREACHGENIUS_DRIPS.public.zipcodes z 
                            ON TRY_CAST(c.personal_zip AS INTEGER) = z.postal_code
                        {whereClause}
                        GROUP BY TRY_CAST(c.personal_zip as integer), z.latitude, z.longitude
                        ORDER BY zip_count DESC;";
            var result = await conn.QueryAsync<Heatmap>(sql, parameters);
            var heatmaps = result.ToList();

            _cache.Set(cacheKey, heatmaps, GetCacheOptionsForMidnight());
            return heatmaps;
        }

        public async Task<int> GetTotalUsersAsync(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            string cacheKey = $"TotalUsers_{schemaName}";
            //if (_cache.TryGetValue(cacheKey, out int cachedTotal))
            //{
            //    return cachedTotal;
            //}

            var filters = new List<string>();

            var parameters = new DynamicParameters();

            if (startDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) >= :StartDate");
                parameters.Add("StartDate", startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) <= :EndDate");
                parameters.Add("EndDate", endDate.Value.Date);
            }
            var whereClause = "WHERE " + string.Join(" AND ", filters);

            using var conn = _dbFactory.GetConnection();
            int totalUsers = await conn.ExecuteScalarAsync<int>($"SELECT count(distinct first_name, last_name, mobile_phone) FROM {DbName}.{schemaName}.campaign {whereClause}", parameters);
            _cache.Set(cacheKey, totalUsers, GetCacheOptionsForMidnight());
            return totalUsers;
        }

        public async Task<List<Campaign>> GetAll(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            string cacheKey = $"AllCampaigns_{schemaName}";
            //if (_cache.TryGetValue(cacheKey, out List<Campaign>? cachedCampaigns) && cachedCampaigns != null)
            //{
            //    return cachedCampaigns;
            //}

            var filters = new List<string>();

            var parameters = new DynamicParameters();

            if (startDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) >= :StartDate");
                parameters.Add("StartDate", startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) <= :EndDate");
                parameters.Add("EndDate", endDate.Value.Date);
            }
            var whereClause = "WHERE " + string.Join(" AND ", filters);

            using var conn = _dbFactory.GetConnection();
            var result = await conn.QueryAsync<Campaign>($"SELECT * FROM {DbName}.{schemaName}.campaign {whereClause}", parameters);
            var campaigns = result.ToList();

            _cache.Set(cacheKey, campaigns, GetCacheOptionsForMidnight());
            return campaigns;
        }

        public async Task<List<CampaignEnriched>> GetAllEnriched(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            string cacheKey = $"AllCampaignsEnriched_{schemaName}";

            //if (_cache.TryGetValue(cacheKey, out List<CampaignEnriched>? cached) && cached != null)
            //    return cached;

            var filters = new List<string>();
            var parameters = new DynamicParameters();

            if (startDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) >= :StartDate");
                parameters.Add("StartDate", startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) <= :EndDate");
                parameters.Add("EndDate", endDate.Value.Date);
            }

            var whereClause = filters.Any() ? "WHERE " + string.Join(" AND ", filters) : "";

            using var conn = _dbFactory.GetConnection();

            // Step 1: Fetch campaign data
            var campaignQuery = $"SELECT * FROM {DbName}.{schemaName}.campaign {whereClause}";
            var campaigns = (await conn.QueryAsync<Campaign>(campaignQuery, parameters)).ToList();

            // Step 2: Fetch all enrichment score columns
            string campaignTable = $"{DbName}.{schemaName}.campaign";

            var couponScores = await GetScoreColumn(conn, "GET_COUPON_CONVERSION_SCORE", campaignTable);
            var emailScores = await GetScoreColumn(conn, "GET_EMAIL_ENGAGEMENT_SCORE", campaignTable);
            var financialScores = await GetScoreColumn(conn, "GET_FINANCIAL_OVEREXTENTION_SCORE", campaignTable);
            var impulseScores = await GetScoreColumn(conn, "GET_IMPULSE_BUY_SCORE", campaignTable);
            var influencerScores = await GetScoreColumn(conn, "GET_INFLUENCER_RESPONSE_SCORE", campaignTable);
            var smsScores = await GetScoreColumn(conn, "GET_SMS_ENGAGEMENT_SCORE", campaignTable);
            var subscriptionScores = await GetScoreColumn(conn, "GET_SUBSCRIPTION_PURCHASE_SCORE", campaignTable);

            // Step 3: Merge all into CampaignEnriched list
            var enrichedList = campaigns.Select((c, i) =>
            {
                var enriched = new CampaignEnriched();

                // Copy all base properties using reflection
                foreach (var prop in typeof(Campaign).GetProperties())
                {
                    var value = prop.GetValue(c);
                    typeof(CampaignEnriched).GetProperty(prop.Name)?.SetValue(enriched, value);
                }

                // Set enrichment scores
                enriched.CouponConversionScore = couponScores.ElementAtOrDefault(i);
                enriched.EmailEngagementScore = emailScores.ElementAtOrDefault(i);
                enriched.FinancialOverextensionScore = financialScores.ElementAtOrDefault(i);
                enriched.ImpulseBuyScore = impulseScores.ElementAtOrDefault(i);
                enriched.InfluencerResponseScore = influencerScores.ElementAtOrDefault(i);
                enriched.SmsEngagementScore = smsScores.ElementAtOrDefault(i);
                enriched.SubscriptionPurchaseScore = subscriptionScores.ElementAtOrDefault(i);

                return enriched;
            }).ToList();

            _cache.Set(cacheKey, enrichedList, GetCacheOptionsForMidnight());
            return enrichedList;
        }

        private async Task<List<double>> GetScoreColumn(IDbConnection conn, string procedureName, string campaignTable)
        {
            string sql = $"CALL OUTREACHGENIUS_DRIPS.PUBLIC.{procedureName}(:CAMPAIGN_TABLE)";
            var parameters = new DynamicParameters();
            parameters.Add("CAMPAIGN_TABLE", campaignTable);

            var result = await conn.QueryAsync<double>(sql, parameters);
            return result.ToList();
        }

        public async Task<int> GetNewUsersAsync(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            string cacheKey = $"NewUsers_{schemaName}";
            //if (_cache.TryGetValue(cacheKey, out int cachedNewUsers))
            //{
            //    return cachedNewUsers;
            //}

            var filters = new List<string>();

            var parameters = new DynamicParameters();

            if (startDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) >= :StartDate");
                parameters.Add("StartDate", startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) <= :EndDate");
                parameters.Add("EndDate", endDate.Value.Date);
            }
            var whereClause = "WHERE " + string.Join(" AND ", filters);

            try
            {
                using var conn = _dbFactory.GetConnection();
                var sql = $@"
                            SELECT
                                COUNT(distinct first_name, last_name, mobile_phone) 
                            FROM 
                                {DbName}.{schemaName}.campaign 
                            {whereClause}";
                
                int newUsers = await conn.ExecuteScalarAsync<int>(sql, parameters);
                _cache.Set(cacheKey, newUsers, GetCacheOptionsForMidnight());
                return newUsers;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<UsageData>> GetNewUsersOverPast7Days(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            string cacheKey = $"NewUsersOverPast7Days_{schemaName}";
            //if (_cache.TryGetValue(cacheKey, out List<UsageData>? cachedUsersOver7Days))
            //{
            //    return cachedUsersOver7Days ?? new List<UsageData>();
            //}

            var filters = new List<string>();

            var parameters = new DynamicParameters();

            if (startDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) >= :StartDate");
                parameters.Add("StartDate", startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                filters.Add("TO_DATE(created_at) <= :EndDate");
                parameters.Add("EndDate", endDate.Value.Date);
            }
            var whereClause = "WHERE " + string.Join(" AND ", filters);

            try
            {
                // Define the SQL query with the provided schema name.
                var query = $@" SELECT 
                                    TO_DATE(created_at) AS UsageDate,  -- Handles both 'YYYY-MM-DD' and full timestamp format
                                    COUNT(DISTINCT first_name || last_name || mobile_phone) AS UsageCount
                                FROM 
                                    {DbName}.{schemaName}.campaign
                                {whereClause}
                                GROUP BY 
                                    TO_DATE(created_at)
                                ORDER BY 
                                    UsageDate;";

                using var conn = _dbFactory.GetConnection();
                var results = await conn.QueryAsync<UsageData>(query, parameters);
                _cache.Set(cacheKey , results, GetCacheOptionsForMidnight());

                return results.ToList();
            }
            catch
            {
                return new List<UsageData>();
            }
        }

        public async Task<GenderVariance> GetGenderVariance(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var filters = new List<string>();
            
                var parameters = new DynamicParameters();

                if (startDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) >= :StartDate");
                    parameters.Add("StartDate", startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) <= :EndDate");
                    parameters.Add("EndDate", endDate.Value.Date);
                }
                var whereClause = "WHERE " + string.Join(" AND ", filters);

                var sql = $@" SELECT 
                              CASE 
                                WHEN gender = 'M' THEN 'Male'
                                WHEN gender = 'F' THEN 'Female'
                                ELSE 'Other'
                              END AS gender_full,
                              ROUND((COUNT(*) * 100.0) / SUM(COUNT(*)) OVER (), 2) AS percentage
                            FROM 
                                {DbName}.{schemaName}.campaign
                            {whereClause} 
                            GROUP BY 
                              CASE 
                                WHEN gender = 'M' THEN 'Male'
                                WHEN gender = 'F' THEN 'Female'
                                ELSE 'Other'
                              END;";

                // You can use Query<dynamic> if you don't want to create a temporary class
                using var conn = _dbFactory.GetConnection();
                var results = (await conn.QueryAsync(sql, parameters)).ToList();

                // Create a new GenderVariance object
                var genderVariance = new GenderVariance();

                foreach (var row in results)
                    if (row.GENDER_FULL == "Male")
                        genderVariance.Male = Convert.ToInt32(row.PERCENTAGE);
                    else if (row.GENDER_FULL == "Female")
                        genderVariance.Female = Convert.ToInt32(row.PERCENTAGE);

                // Now genderVariance contains the percentage for Male and Female
                return genderVariance;
            }
            catch
            {
                return new GenderVariance();
            }
        }

        public async Task<int> GetAverageIncome(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var filters = new List<string>();

                var parameters = new DynamicParameters();

                if (startDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) >= :StartDate");
                    parameters.Add("StartDate", startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) <= :EndDate");
                    parameters.Add("EndDate", endDate.Value.Date);
                }
                var whereClause = "WHERE " + string.Join(" AND ", filters);

                var sql = $@" SELECT ROUND(
                                     AVG(
                                       CASE 
                                         WHEN INCOME_RANGE ILIKE '% to %' THEN 
                                           (
                                             TO_NUMBER(REPLACE(REPLACE(SPLIT_PART(INCOME_RANGE, ' to ', 1), '$', ''), ',', '')) +
                                             TO_NUMBER(REPLACE(REPLACE(SPLIT_PART(INCOME_RANGE, ' to ', 2), '$', ''), ',', ''))
                                           ) / 2
                                         WHEN INCOME_RANGE ILIKE '%+%' THEN 
                                           TO_NUMBER(REPLACE(REPLACE(REPLACE(INCOME_RANGE, '$', ''), '+', ''), ',', ''))
                                         ELSE NULL
                                       END
                                     ), 0) AS avg_income
                            FROM {DbName}.{schemaName}.CAMPAIGN {whereClause};";

                using var conn = _dbFactory.GetConnection();
                var result = await conn.QuerySingleAsync<int>(sql, parameters);

                return result;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<AgeRange>> GetAgeRange(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var filters = new List<string>();

                var parameters = new DynamicParameters();

                if (startDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) >= :StartDate");
                    parameters.Add("StartDate", startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) <= :EndDate");
                    parameters.Add("EndDate", endDate.Value.Date);
                }
                var whereClause = "WHERE " + string.Join(" AND ", filters);

                var sql = $@"SELECT
                              age_range,
                              COUNT(*) AS count
                            FROM {DbName}.{schemaName}.CAMPAIGN
                            {whereClause} 
                            GROUP BY age_range
                            ORDER BY count DESC;
                            ";

                using var conn = _dbFactory.GetConnection();
                var result = await conn.QueryAsync<AgeRange>(sql, parameters);

                return result.ToList();
            }
            catch
            {
                return new List<AgeRange>();
            }
        }

        public async Task<List<TopicData>> GetTopicBreakdown(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var filters = new List<string>();

                var parameters = new DynamicParameters();

                if (startDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) >= :StartDate");
                    parameters.Add("StartDate", startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) <= :EndDate");
                    parameters.Add("EndDate", endDate.Value.Date);
                }
                var whereClause = "WHERE " + string.Join(" AND ", filters);

                var sql = $@"SELECT
                              topic,
                              COUNT(*) AS count
                            FROM {DbName}.{schemaName}.CAMPAIGN
                            {whereClause} 
                            GROUP BY topic
                            ORDER BY count DESC;";

                using var conn = _dbFactory.GetConnection();
                var result = await conn.QueryAsync<TopicData>(sql, parameters);

                return result.ToList();
            }
            catch
            {
                return new List<TopicData>();
            }
        }

        public async Task<List<ProfessionData>> GetProfessionBreakdown(string schemaName, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var filters = new List<string>();

                var parameters = new DynamicParameters();

                if (startDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) >= :StartDate");
                    parameters.Add("StartDate", startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    filters.Add("TO_DATE(created_at) <= :EndDate");
                    parameters.Add("EndDate", endDate.Value.Date);
                }
                var whereClause = "WHERE " + string.Join(" AND ", filters);

                var sql = $@"SELECT
                              CASE 
                                WHEN TRIM(job_title) = '' THEN 'N/A'
                                ELSE job_title
                              END AS profession,
                              COUNT(*) AS count
                            FROM {DbName}.{schemaName}.CAMPAIGN
                            {whereClause} 
                            GROUP BY
                              CASE 
                                WHEN TRIM(job_title) = '' THEN 'N/A'
                                ELSE job_title
                              END
                            ORDER BY count DESC;";

                using var conn = _dbFactory.GetConnection();
                var result = await conn.QueryAsync<ProfessionData>(sql, parameters);

                return result.ToList();
            }
            catch
            {
                return new List<ProfessionData>();
            }
        }
    }
}
