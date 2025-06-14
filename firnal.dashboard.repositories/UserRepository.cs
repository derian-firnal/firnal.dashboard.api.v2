using Dapper;
using firnal.dashboard.data;
using firnal.dashboard.repositories.Interfaces;

namespace firnal.dashboard.repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SnowflakeDbConnectionFactory _dbFactory;
        private const string DbName = "OUTREACHGENIUS_DRIPS";
        private const string SchemaName = "PUBLIC";

        public UserRepository(SnowflakeDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<User?> GetUserByEmail(string? email)
        {
            using var conn = _dbFactory.GetConnection();

            string query = $@"
                SELECT 
                    u.Id, 
                    u.UserName, 
                    u.Email, 
                    u.PasswordHash,
                    us.UserId AS SchemaUserId, 
                    us.SchemaName,
                    r.Id,
                    r.Name
                FROM {DbName}.{SchemaName}.Users u
                LEFT JOIN {DbName}.{SchemaName}.UserSchemas us ON u.Id = us.UserId
                LEFT JOIN {DbName}.{SchemaName}.UserRoles ur ON u.Id = ur.UserId
                LEFT JOIN {DbName}.{SchemaName}.Roles r ON ur.RoleId = r.Id
                WHERE LOWER(u.Email) = LOWER(:Email)";

            // Dictionary to group user rows (when a user has multiple schemas)
            var userDictionary = new Dictionary<string, User>();

            var result = await conn.QueryAsync<User, UserSchema, Role, User>(
                query,
                (user, userSchema, role) =>
                {
                    if (!userDictionary.TryGetValue(user.Id, out var currentUser))
                    {
                        currentUser = user;
                        currentUser.Schemas = new List<UserSchema>();
                        // Assign role name from the Role object if available.
                        if (role != null)
                        {
                            currentUser.RoleName = role.Name;
                        }
                        userDictionary.Add(user.Id, currentUser);
                    }

                    // Add the schema if it exists.
                    if (userSchema != null && !string.IsNullOrEmpty(userSchema.SchemaName))
                    {
                        currentUser?.Schemas?.Add(userSchema);
                    }

                    return currentUser;
                },
                new { Email = email },
                splitOn: "SchemaUserId,Id" // Splits: UserSchema starts at SchemaUserId; Role starts at Id.
            );

            var userResult = userDictionary.Values.FirstOrDefault();

            return userResult;
        }

        public async Task<List<User>> GetAllUsers()
        {
            using var conn = _dbFactory.GetConnection();

            string query = $@"
                            SELECT 
                                u.Id, 
                                u.UserName, 
                                u.Email, 
                                u.PasswordHash,
                                us.UserId AS SchemaUserId, 
                                us.SchemaName,
                                r.Id,
                                r.Name
                            FROM {DbName}.{SchemaName}.Users u
                            LEFT JOIN {DbName}.{SchemaName}.UserSchemas us ON u.Id = us.UserId
                            LEFT JOIN {DbName}.{SchemaName}.UserRoles ur ON u.Id = ur.UserId
                            LEFT JOIN {DbName}.{SchemaName}.Roles r ON ur.RoleId = r.Id";

            // Use a dictionary keyed by email to ensure each email appears only once
            var userDictionary = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

            var result = await conn.QueryAsync<User, UserSchema, Role, User>(query, (user, userSchema, role) =>
            {
                // Use email as the unique key
                if (!userDictionary.TryGetValue(user.Email, out var currentUser))
                {
                    currentUser = user;
                    currentUser.Schemas = new List<UserSchema>();
                    if (role != null)
                    {
                        currentUser.RoleName = role.Name;
                    }
                    userDictionary.Add(user.Email, currentUser);
                }

                // If a schema is present and not already added, add it to the user's list
                if (userSchema != null && !string.IsNullOrEmpty(userSchema.SchemaName))
                {
                    if (!currentUser.Schemas.Any(s => s.SchemaName == userSchema.SchemaName))
                    {
                        currentUser.Schemas.Add(userSchema);
                    }
                }

                return currentUser;
            }, splitOn: "SchemaUserId,Id");

            return userDictionary.Values.OrderBy(r => r.RoleName).ThenBy(r => r.UserName).ToList();
        }

        public async Task<bool> UpdateUser(User updatedUser)
        {
            using var conn = _dbFactory.GetConnection();
            
            using var tran = conn.BeginTransaction();
            try
            {
                // First, remove the user's existing schema and role associations.
                string deleteUserSchemasQuery = $@"DELETE FROM {DbName}.{SchemaName}.UserSchemas WHERE UserId = :UserId";
                string deleteUserRolesQuery = $@"DELETE FROM {DbName}.{SchemaName}.UserRoles WHERE UserId = :UserId";

                await conn.ExecuteAsync(deleteUserSchemasQuery, new { UserId = updatedUser.Id }, transaction: tran);
                await conn.ExecuteAsync(deleteUserRolesQuery, new { UserId = updatedUser.Id }, transaction: tran);

                // Retrieve the Role's id from the Roles table based on the new role name.
                string getRoleIdQuery = $@"SELECT Id FROM {DbName}.{SchemaName}.Roles WHERE Name = :RoleName";

                var roleId = await conn.QuerySingleOrDefaultAsync<string>(
                    getRoleIdQuery,
                    new { RoleName = updatedUser.RoleName },
                    transaction: tran
                );

                // If a valid role id was found, insert the new user role.
                if (!string.IsNullOrEmpty(roleId))
                {
                    string insertUserRoleQuery = $@"INSERT INTO {DbName}.{SchemaName}.UserRoles (UserId, RoleId) VALUES (:UserId, :RoleId)";
                    await conn.ExecuteAsync(
                        insertUserRoleQuery,
                        new { UserId = updatedUser.Id, RoleId = roleId },
                        transaction: tran
                    );
                }

                // Insert each new schema if any exist.
                if (updatedUser.Schemas != null && updatedUser.Schemas.Any())
                {
                    string insertUserSchemaQuery = $@"INSERT INTO {DbName}.{SchemaName}.UserSchemas (UserId, SchemaName) VALUES (:UserId, :SchemaName)";
                    foreach (var schema in updatedUser.Schemas)
                    {
                        await conn.ExecuteAsync(
                            insertUserSchemaQuery,
                            new { UserId = updatedUser.Id, SchemaName = schema.SchemaName },
                            transaction: tran
                        );
                    }
                }

                tran.Commit();

                return true;
            }
            catch (Exception ex)
            {
                tran.Rollback();
                
                throw;
            }
        }

        public async Task<bool> DeleteUser(string userId)
        {
            using var conn = _dbFactory.GetConnection();
            
            using var tran = conn.BeginTransaction();
            try
            {
                // Delete associated schemas for the user.
                string deleteSchemasQuery = $@"DELETE FROM {DbName}.{SchemaName}.UserSchemas WHERE UserId = :UserId";
                await conn.ExecuteAsync(deleteSchemasQuery, new { UserId = userId }, transaction: tran);

                // Delete associated roles for the user.
                string deleteRolesQuery = $@"DELETE FROM {DbName}.{SchemaName}.UserRoles WHERE UserId = :UserId";
                await conn.ExecuteAsync(deleteRolesQuery, new { UserId = userId }, transaction: tran);

                // Finally, delete the user record.
                string deleteUserQuery = $@"DELETE FROM {DbName}.{SchemaName}.Users WHERE Id = :UserId";
                int rowsAffected = await conn.ExecuteAsync(deleteUserQuery, new { UserId = userId }, transaction: tran);

                tran.Commit();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                tran.Rollback();
                // Consider logging the exception
                throw;
            }
        }
    }
}
