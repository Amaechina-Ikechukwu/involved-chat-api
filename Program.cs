using System.Text;
using HealthChecks.UI.Client;
using Involved_Chat.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver; // IMongoClient registration
using HealthChecks.MongoDb;
using Involved_Chat.Models;
using Involved_Chat.Services; // MongoDB health check extension
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var jwtKey = builder.Configuration["Jwt:Key"];

// Ensure the configured JWT key meets the minimum size for HS256 (128 bits / 16 bytes)
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("Missing configuration value 'Jwt:Key'. Please set a secure key in configuration or via environment variable.");
}

byte[] jwtKeyBytes;
try
{
    // Try base64 decode first (allows using an exact-size secret)
    jwtKeyBytes = Convert.FromBase64String(jwtKey);
}
catch
{
    // Not base64, fall back to UTF8 bytes
    jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);
}

if (jwtKeyBytes.Length * 8 < 128)
{
    // Provide actionable guidance: either use a longer passphrase or a base64-encoded 16+ byte random key.
    var utf8Bits = Encoding.UTF8.GetBytes(jwtKey).Length * 8;
    throw new InvalidOperationException($"Jwt:Key must be at least 128 bits (16 bytes) for HS256. Current key size: {jwtKeyBytes.Length * 8} bits. " +
        "Fix: set 'Jwt:Key' to a 16+ byte secret (e.g. a 16+ byte base64 string or a long passphrase). " +
        "To generate a secure base64 key: use `openssl rand -base64 24` or `dotnet user-secrets set \"Jwt:Key\" \"$(Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)))\"`.");
}
var mongoConn = builder.Configuration["MongoDbSettings:ConnectionString"];
// Generate lowercase URLs (makes [controller] tokens lowercase in routes and Swagger)
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Configure OpenAPI/NSwag and add a JWT Bearer security scheme so the Swagger UI shows an Authorize input
builder.Services.AddOpenApiDocument(options =>
{
    // Optional: set basic document info
    options.PostProcess = document =>
    {
        document.Info.Title = "Involved Chat API";
    };

    options.AddSecurity("JWT", new NSwag.OpenApiSecurityScheme
    {
        Type = NSwag.OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = NSwag.OpenApiSecurityApiKeyLocation.Header,
        Description = "Enter 'Bearer {token}' or just paste the JWT token."
    });

    // Require the scheme globally for all operations (optional)
    options.OperationProcessors.Add(new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor("JWT"));
});
// Register application services
builder.Services.AddScoped<Involved_Chat.Services.AuthService>();
builder.Services.AddScoped<Involved_Chat.Services.MessageService>();
// Authorization should be added before building the app so middleware is available
builder.Services.AddAuthorization();
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings")); //getting mongo settings
// Register the MongoDB client as a singleton so it can be injected where needed
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
builder.Services.AddSingleton<MongoDbContext>(); //Register context

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddMongoDb(
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "ready" }
    );
// HealthChecks with UI
builder.Services
    .AddHealthChecksUI(options =>
    {
        options.SetEvaluationTimeInSeconds(10); // refresh interval
        options.AddHealthCheckEndpoint("default", "/health/ready"); // endpoint to check
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            // Use the already-decoded key bytes so base64-configured keys are honored.
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes)
        };

        // 🔑 Allow JWT from SignalR query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Serve the OpenAPI document at /openapi/v1.json (default is /swagger/v1/swagger.json; customizing for consistency)
    app.UseOpenApi(settings =>
    {
        settings.Path = "/openapi/v1.json";
    });

    // Serve Swagger UI (NSwag UI) and point it to the above document
    app.UseSwaggerUi(settings =>
    {
        settings.DocumentPath = "/openapi/v1.json"; // Must match UseOpenApi path
        // The Authorize button appears because of the security scheme defined in AddOpenApiDocument
    });
}
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";   // where to access the UI
    options.ApiPath = "/health-ui-api"; // backend API for the UI
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
