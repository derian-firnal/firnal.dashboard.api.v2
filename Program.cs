using firnal.dashboard.api;
using firnal.dashboard.data;
using firnal.dashboard.repositories;
using firnal.dashboard.repositories.Interfaces;
using firnal.dashboard.repositories.v2;
using firnal.dashboard.repositories.v2.Interfaces;
using firnal.dashboard.services;
using firnal.dashboard.services.Interfaces;
using firnal.dashboard.services.v2;
using firnal.dashboard.services.v2.Interfaces;

var builder = WebApplication.CreateBuilder(args);

#if (RELEASE)
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8081"; // Default to 8080 if PORT is not set
    builder.WebHost.UseUrls($"http://*:{port}");
#endif

builder.Services.AddMemoryCache();

// Add services to the container.
builder.Services.AddSingleton<SnowflakeDbConnectionFactory>();
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<ISchemaRepository, SchemaRepository>();
builder.Services.AddScoped<ISchemaService, SchemaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IConsumerGraphService, ConsumerGraphService>();
builder.Services.AddScoped<ITwilioService, TwilioService>();
builder.Services.AddScoped<IAudienceRepository, AudienceRepository>();
builder.Services.AddScoped<IAudienceService, AudienceService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// JWT Bearer authentication configuration
builder.AddJWT();

// Swagger with JWT configuration
builder.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseCors("AllowAllOrigins"); // Enable CORS globally

app.UseAuthentication();

// Enable Swagger for Production (Railway)
if (app.Environment.IsDevelopment() || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger at root
    });

    Console.WriteLine("Swagger enabled in Railway.");
}

// Remove HTTPS redirection when running on Railway
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")))
{
    Console.WriteLine("Running on Railway - Skipping HTTPS Redirection");
}
else
{
    app.UseHttpsRedirection(); // Enable locally
}

//app.MapGet("/", () => "Hello from Railway!");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
