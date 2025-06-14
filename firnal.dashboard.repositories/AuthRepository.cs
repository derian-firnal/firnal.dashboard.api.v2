using Dapper;
using firnal.dashboard.data;
using firnal.dashboard.repositories.Interfaces;

namespace firnal.dashboard.repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly SnowflakeDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;

        private const string DbName = "OUTREACHGENIUS_DRIPS";
        private const string SchemaName = "PUBLIC";

        public AuthRepository(SnowflakeDbConnectionFactory dbFactory, IUserRepository userRepository)
        {
            _dbFactory = dbFactory;
            _userRepository = userRepository;
        }

        public async Task<User?> AuthenticateUser(string email, string password)
        {
            var user = await _userRepository.GetUserByEmail(email);
            
            // Verify the password if a user was found.
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return user;

            return null;
        }

        public async Task<string?> RegisterUser(string email, string username, string password, string role, List<string>? schemas)
        {
            using var conn = _dbFactory.GetConnection();
            
            using var transaction = conn.BeginTransaction();

            try
            {
                string userId = Guid.NewGuid().ToString();
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // Step 1: Insert User using colon-prefixed parameters
                string userInsertQuery = $@"INSERT INTO {DbName}.{SchemaName}.Users (ID, USERNAME, EMAIL, PASSWORDHASH) VALUES (:ID, :USERNAME, :EMAIL, :PASSWORDHASH)";

                int userResult = await conn.ExecuteAsync(userInsertQuery, new
                {
                    ID = userId,
                    USERNAME = username,
                    EMAIL = email,
                    PASSWORDHASH = passwordHash
                }, transaction);

                if (userResult <= 0) throw new Exception("Failed to insert user.");

                // Step 2: Lookup Role ID
                string roleQuery = $@"SELECT ID FROM {DbName}.{SchemaName}.ROLES WHERE NAME = :Name";

                var roleId = await conn.ExecuteScalarAsync<string>(roleQuery, new
                {
                    Name = role
                }, transaction);

                if (string.IsNullOrEmpty(roleId)) throw new Exception("Role not found.");

                // Step 3: Insert User Role
                string roleInsertQuery = $@"INSERT INTO {DbName}.{SchemaName}.UserRoles (UserId, RoleId) VALUES (:UserId, :RoleId)";

                int roleResult = await conn.ExecuteAsync(roleInsertQuery, new
                {
                    UserId = userId,
                    RoleId = roleId
                }, transaction);

                if (roleResult <= 0) throw new Exception("Failed to insert user role.");

                // Step 4: Insert User Schema Mappings
                string schemaInsertQuery = $@"INSERT INTO {DbName}.{SchemaName}.UserSchemas (UserId, SchemaName) VALUES (:UserId, :SchemaName)";

                if (schemas != null && schemas.Count > 0)
                {
                    foreach (var schema in schemas)
                    {
                        int schemaResult = await conn.ExecuteAsync(schemaInsertQuery, new
                        {
                            UserId = userId,
                            SchemaName = schema
                        }, transaction);

                        if (schemaResult <= 0) throw new Exception($"Failed to insert schema access for {schema}.");
                    }
                }

                // Commit transaction on success
                transaction.Commit();
                return userId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error in RegisterUser: {ex.Message}");
                return null;
            }
        }
    }
}
