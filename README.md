# Involved Chat API

A lightweight ASP.NET Core 9.0 backend service providing the foundation for a chat application with JWT-based authentication and MongoDB persistence.

> Status: Early scaffolding phase – authentication service and data models exist; controllers / hubs are not yet implemented.

## ✨ Features (Current)
- .NET 9 minimal hosting
- MongoDB integration via a simple context wrapper (`MongoDbContext`)
- User & Message domain models
- Basic authentication service (`AuthService`) with:
  - SHA-256 password hashing (no salt yet – see Improvements)
  - JWT issuance (Issuer / Audience / Key from config)
  - 7‑day token lifetime
- OpenAPI (Swagger UI) auto-exposed in Development environment
- Environment-based configuration (`appsettings*.json`)

## 🧱 Architecture Overview
```
root
├── Program.cs              -> App bootstrap, DI, JWT auth, OpenAPI
├── Data/
│   └── MongoDbContext.cs   -> Mongo connection + typed collections
├── Models/
│   ├── Users.cs            -> User entity (Id, Username, Email, PasswordHash, CreatedAt)
│   └── Messages.cs         -> Message entity (FromUserId, ToUserId, Content, SentAt, IsRead)
├── Services/
│   └── AuthService.cs      -> Registration & Login + JWT issuing
├── appsettings.json        -> Mongo + JWT config (placeholders)
└── appsettings.Development.json -> Dev logging overrides
```

Planned (referenced but not yet implemented):
- SignalR hub at `/chatHub` (JWT extraction from query string is already wired in the JWT bearer events)
- Controller endpoints (currently `app.MapControllers();` is commented out in `Program.cs` — uncomment after adding controllers)

## 🛠 Tech Stack
- **Language / Runtime:** .NET 9 (ASP.NET Core)
- **Database:** MongoDB
- **Auth:** JWT (HMAC SHA-256)
- **Documentation:** OpenAPI / Swagger UI (Development)

## ⚙️ Configuration
Defined in `appsettings.json` (replace `PLACEHOLDER` values):
```jsonc
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017", // example
    "DatabaseName": "involved"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_SECURE_RANDOM_32+_CHARS",
    "Issuer": "Involved",
    "Audience": "InvolvedUsers"
  }
}
```
You can override via environment variables (ASP.NET Core standard mapping) or `appsettings.Development.json` for local dev.

Note: The `appsettings.Development.json` in this project contains a 32-character development JWT key to satisfy HS256 minimum key length. Do NOT commit production secrets; use environment variables or a secrets manager for real deployments.

### Recommended Secure Key Generation
```powershell
# Windows PowerShell
[Convert]::ToBase64String((New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes(32))
```

## ▶️ Running Locally
From the repository root:
```powershell
# Restore
dotnet restore

# Run (Development)
dotnet run

# (Optional) Build
dotnet build -c Release
```
Navigate to: `https://localhost:5001` (or the HTTPS port shown in console)
Swagger UI (Development): `https://localhost:5001/swagger` or as mapped by OpenAPI configuration: `/openapi/v1.json` for the raw document.

## 🔐 Authentication Flow
`AuthService` exposes two core operations internally (not yet surfaced via a controller):
- `RegisterAsync(username, email, password)` – hashes password with SHA-256 and stores user.
- `LoginAsync(email, password)` – validates credentials and returns a JWT with claims:
  - `nameid` (User Id)
  - `name` (Username)
  - `email` (Email)

JWT settings:
- Expiry: 7 days
- Signing: SymmetricSecurityKey (HMAC-SHA256)
- Validation: Issuer, Audience, Lifetime, and Signing Key enforced

SignalR readiness: The JWT bearer events are configured to read an `access_token` from the query string for paths beginning with `/chatHub` (hub not yet added).

## 🧾 Data Models
### User
| Field | Type | Notes |
|-------|------|-------|
| Id | string (ObjectId) | MongoDB primary key |
| Username | string | Unique expectation (not yet enforced) |
| Email | string | Uniqueness enforced during registration |
| PasswordHash | string | SHA-256 hash (no salt) |
| CreatedAt | DateTime (UTC) | Set on creation |

### Message
| Field | Type | Notes |
|-------|------|-------|
| Id | string (ObjectId) | MongoDB primary key |
| FromUserId | string (ObjectId) | Sender reference |
| ToUserId | string (ObjectId) | Recipient reference |
| Content | string | Message body |
| SentAt | DateTime (UTC) | Defaults to now |
| IsRead | bool | Read status |

## 🚧 Missing / To Be Implemented
- Public API controllers for auth (register/login) & messaging
- SignalR chat hub (`/chatHub`)
- Message persistence endpoints (list, send, mark read)
- User lookup / profile endpoints
- Validation & error handling middleware
- Logging enrichment / structured logging
- Dockerfile + container orchestration
- Unit / integration tests

## ✅ Suggested Next Steps
1. Add `AuthController` exposing register/login endpoints
2. Add SignalR Hub & map to `/chatHub`
3. Introduce DTOs to avoid exposing `PasswordHash`
4. Add password hashing with salt & pepper (e.g., BCrypt / PBKDF2 / Argon2)
5. Introduce refresh tokens if long-lived sessions are required
6. Add rate limiting for auth endpoints
7. Implement repository or service abstractions for testability
8. Add automated tests (xUnit / NUnit) + GitHub Actions CI
9. Provide Dockerfile & docker-compose (Mongo + API)
10. Add pagination / indexing for message queries

## 🧪 Example Controller Sketch (Future)
```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var user = await _auth.RegisterAsync(req.Username, req.Email, req.Password);
        return Ok(new { user.Id, user.Username, user.Email, user.CreatedAt });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var token = await _auth.LoginAsync(req.Email, req.Password);
        return token is null ? Unauthorized() : Ok(new { token });
    }
}
```

## 🔐 Security Considerations (Planned Improvements)
- Replace raw SHA-256 hashing with a slow hashing algorithm (Argon2id recommended)
- Enforce password complexity & length rules
- Add account lockout / throttling
- Ensure HTTPS enforced in production (HSTS)
- Consider secret storage (Azure Key Vault / AWS Secrets Manager) for JWT key & Mongo credentials

## 🧭 License
Add a license (e.g., MIT) at this stage if open sourcing.

## 🙌 Contributing
Not yet open for external contributions. Once stabilized: fork → branch → PR.

---
Generated README based on current repository contents (Program, Models, Services, Config). Update as new controllers and hubs are added.
