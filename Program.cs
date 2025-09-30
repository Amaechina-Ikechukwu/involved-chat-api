using System.Text;
using Involved_Chat.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddOpenApi();
// Register application services
builder.Services.AddScoped<Involved_Chat.Services.AuthService>();
// Authorization should be added before building the app so middleware is available
builder.Services.AddAuthorization();
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings")); //getting mongo settings
builder.Services.AddSingleton<MongoDbContext>(); //Rgister context
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
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
