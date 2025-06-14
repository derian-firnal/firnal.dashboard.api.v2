using Dapper;
using firnal.dashboard.data;
using firnal.dashboard.repositories.Interfaces;

namespace firnal.dashboard.repositories
{
    public class SchemaRepository : ISchemaRepository
    {
        private readonly SnowflakeDbConnectionFactory _dbFactory;

        public SchemaRepository(SnowflakeDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<string>> GetAll()
        {
            using var conn = _dbFactory.GetConnection();

            var sql = $"SELECT schema_name FROM OUTREACHGENIUS_DRIPS.INFORMATION_SCHEMA.SCHEMATA WHERE schema_name NOT IN ('PUBLIC', 'INFORMATION_SCHEMA');";
            var result = await conn.QueryAsync<string>(sql);

            return result.ToList();
        }
    }
}
