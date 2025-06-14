namespace firnal.dashboard.data
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? JwtToken { get; set; }
        public string? RoleName { get; set; }
        public List<UserSchema>? Schemas { get; set; }
    }

    public class UserSchema
    {
        public string? UserId { get; set; }
        public string? SchemaName { get; set; }
    }
}
