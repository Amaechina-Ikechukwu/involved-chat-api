using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Involved_Chat.Data;
using Involved_Chat.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using Google.Cloud.SecretManager.V1;
namespace Involved_Chat.Services
{
    public class AuthService
    {
        private readonly MongoDbContext _context;
        private readonly IConfiguration _config;
    private readonly string _jwtKey;

       

        // --- Helper Method ---
        private static string GetSecret(string secretId)
        {
            var client = SecretManagerServiceClient.Create();
            var secretName = new SecretVersionName("tangle2", secretId, "latest");
            var result = client.AccessSecretVersion(secretName);
            return result.Payload.Data.ToStringUtf8();
        }
        public AuthService(MongoDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _jwtKey = GetSecret("jwt_key");
        }

        private static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public async Task<User?> RegisterAsync(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("username is required", nameof(username));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email is required", nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password is required", nameof(password));
            // Ensure username is unique (case-insensitive)
            var usernameFilter = Builders<User>.Filter.Regex(u => u.Username, new BsonRegularExpression($"^{Regex.Escape(username)}$", "i"));
            if (await _context.Users.Find(usernameFilter).AnyAsync())
                throw new Exception("Username already exists");

            if (await _context.Users.Find(u => u.Email == email).AnyAsync())
                throw new Exception("Email already exists");

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password)
            };

            await _context.Users.InsertOneAsync(user);
            return user;
        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email is required", nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password is required", nameof(password));
            var user = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null || user.PasswordHash != HashPassword(password))
                return null;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var jwtKey = _jwtKey;
            if (string.IsNullOrEmpty(jwtKey)) throw new InvalidOperationException("JWT key is not configured. Please set Jwt:Key in configuration.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }
    }
}
