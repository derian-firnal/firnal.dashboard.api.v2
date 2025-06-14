using firnal.dashboard.data;
using firnal.dashboard.repositories.Interfaces;
using firnal.dashboard.services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace firnal.dashboard.services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public AuthService(IConfiguration config, IAuthRepository authRepository)
        {
            _authRepository = authRepository;
            _jwtSecret = config["JwtSettings:Secret"] ?? throw new Exception("jwtSecret string not found.");
            _jwtIssuer = config["JwtSettings:Issuer"] ?? throw new Exception("jwtIssuer string not found.");
            _jwtAudience = config["JwtSettings:Audience"] ?? throw new Exception("jwtAudience string not found.");
        }

        public async Task<User?> AuthenticateUser(string email, string password)
        {
            var user = await _authRepository.AuthenticateUser(email, password);

            if (user != null)
            {
                user.JwtToken = await GenerateJwtToken(user);
                return user;
            }

            return null;
        }

        public async Task<string?> RegisterUser(string email, string username, string password, string role, List<string>? schemas)
        {
            return await _authRepository.RegisterUser(email, username, password, role, schemas);
        }

        private Task<string> GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.RoleName ?? string.Empty)
            };

            // Add a claim for each schema the user has access to
            if (user.Schemas != null)
            {
                foreach (var userSchema in user.Schemas)
                {
                    if (!string.IsNullOrEmpty(userSchema.SchemaName))
                    {
                        claims.Add(new Claim("schema", userSchema.SchemaName));
                    }
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            // Wrap the generated token string in a Task
            return Task.FromResult("Bearer " + new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}
