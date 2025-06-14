using Snowflake.Data.Client;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace firnal.dashboard.data
{
    public class SnowflakeDbConnectionFactory
    {
        private readonly string? _privateKey;
        private readonly string _connectionString;

        public SnowflakeDbConnectionFactory(IConfiguration config)
        {
            _privateKey = config["Snowflake:PrivateKey"];
            _connectionString = config.GetConnectionString("Snowflake") ?? throw new Exception("Snowflake connection string not found.");
        }

        public IDbConnection GetConnection()
        {
            var connection = new SnowflakeDbConnection
            {
                ConnectionString = $"{_connectionString};AUTHENTICATOR=snowflake_jwt;PRIVATE_KEY={_privateKey}"
            };
            connection.Open();
            return connection;
        }
    }
}
