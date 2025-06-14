using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace firnal.dashboard.api
{
    public static class HelperExtensions
    {
        public static IServiceCollection AddJWT(this WebApplicationBuilder builder)
        {
            var secret = builder.Configuration.GetValue<string>("JwtSettings:Secret");
            var issuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer");
            var audience = builder.Configuration.GetValue<string>("JwtSettings:Audience");
            if (!string.IsNullOrEmpty(secret) &&
                !string.IsNullOrEmpty(issuer) &&
                !string.IsNullOrEmpty(audience))
            {
                var key = Encoding.ASCII.GetBytes(secret);

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                });

                return builder.Services;
            }

            throw new Exception("JWT Secret Not Found");
        }

        public static IServiceCollection AddSwaggerGen(this WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                // Define the security scheme for bearer authentication
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                // Apply the security requirement to all endpoints
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
            });

            return builder.Services;
        }
    }
}
