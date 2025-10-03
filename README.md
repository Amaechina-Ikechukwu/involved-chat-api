# Involved — Chat API (Backend)

A lightweight ASP.NET Core 9 backend that provides the foundation for a modern chat / conversation platform.

This repository contains the minimal building blocks: MongoDB-backed models, an authentication service that issues JWTs, and a scaffolded app host (OpenAPI and SignalR readiness). The project is in early development: core services and models exist, but public controllers and the SignalR hub are still being wired up.

## Quick summary
- Runtime: .NET 9 (ASP.NET Core)
- Database: MongoDB
- Auth: JWT (HMAC-SHA256 currently)
- API docs: OpenAPI / Swagger (Development only)

Use this repo as a starting point for implementing a chat backend with token-based auth and SignalR-powered real-time messaging.

## What's in this repo
- `Program.cs` — app bootstrap, DI, JWT auth, OpenAPI setup
- `Data/MongoDbContext.cs` — MongoDB client + typed collections
- `Models/Users.cs`, `Models/Messages.cs` — domain models
- `Services/AuthService.cs` — registration, login, JWT issuance
- `Hubs/ChatHub.cs` — SignalR hub scaffold (hub logic can be extended)
- `Controllers/` — placeholder for future controllers (Auth/Message controllers not yet enabled)
- `appsettings*.json` — configuration templates

## Quickstart (local development)
Make sure .NET 9 SDK and a running MongoDB instance are available.

1. Restore dependencies

```powershell
dotnet restore
```

2. Configure settings

Copy `appsettings.Development.json` or edit `appsettings.json` to set your MongoDB connection and JWT settings. At minimum set a secure `Jwt:Key` for local development.

3. Run the app (Development)

```powershell
dotnet run
```

4. Open the app

- API root: https://localhost:{port} (port shown in console)
- Swagger UI (Development): https://localhost:{port}/swagger

## Configuration
Edit `appsettings.json` or use environment variables. Key sections:

- `MongoDbSettings`:
  - `ConnectionString` — e.g. `mongodb://localhost:27017`
  - `DatabaseName` — e.g. `involved`
- `Jwt`:
  - `Key` — HMAC key (use a secure random secret in production)
  - `Issuer` / `Audience` — token validation values

Example snippet:

```jsonc
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "involved"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_SECURE_RANDOM_32+_CHARS",
    "Issuer": "Involved",
    "Audience": "InvolvedUsers"
  }
}
```

Recommended key generation (PowerShell):

```powershell
[Convert]::ToBase64String((New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes(32))
```

Do not commit production secrets. Use environment variables or a secrets manager.

## API overview (current)
The project currently contains an `AuthService` that performs:

- Register (hashes password and stores a user)
- Login (validates credentials and returns a JWT)

Controllers exposing these operations are not yet enabled — see `Controllers/` and `Program.cs` for where to add them. Example controller sketch (implement under `Controllers/AuthController.cs`):

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

## SignalR notes
The JWT bearer options are preconfigured to allow passing the `access_token` via query string for SignalR connections to the hub path (typically `/chatHub`). This is convenient for browsers that connect via WebSockets and cannot set Authorization headers during the initial handshake.

When you implement the client-side connection, connect like this (JS example):

```javascript
// Web client using @microsoft/signalr
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/chatHub", { accessTokenFactory: () => token })
  .build();

await connection.start();
```

## Data model summary

User (partial):
- Id (ObjectId as string)
- Username
- Email
- PasswordHash (currently SHA-256; see Security section)
- CreatedAt (UTC)

Message (partial):
- Id (ObjectId as string)
- FromUserId, ToUserId
- Content
- SentAt (UTC)
- IsRead (bool)

## Security notes and recommended improvements
Current implementation uses SHA-256 for password hashing (without salt). This is NOT recommended for production.

Planned improvements you should implement before production:

- Replace SHA-256 with a slow, memory-hard password hashing algorithm (Argon2id, BCrypt, or PBKDF2 with strong parameters)
- Add per-user salt and optional application-wide pepper
- Enforce password complexity and rate-limit auth endpoints
- Use HTTPS + HSTS in production and store secrets in a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.)
- Consider refresh tokens for session management

## Roadmap / TODOs
- Add `AuthController` and expose register/login endpoints (PR-ready example in README)
- Implement `MessageController` for sending, listing, and marking messages read
- Harden password hashing and migrate existing users safely
- Add unit & integration tests (xUnit) and CI (GitHub Actions)
- Dockerize API and add a `docker-compose` setup with MongoDB for local dev
- Add pagination, query indexing, and performance tests for message queries

## How you can help / Contributing
- Add controllers and DTOs to avoid exposing `PasswordHash`
- Implement secure password hashing and account policies
- Add tests for `AuthService` and `MessageService`

When contributing: fork → branch → open a pull request. Keep changes focused and include tests for behavior changes.

## Running & debugging tips
- Use `appsettings.Development.json` for local overrides. The project exposes Swagger only in the Development environment.
- If the app doesn't start, check the console for missing configuration values (Mongo connection or JWT key).

## License
Add a license file (e.g., MIT) if you plan to open source this repository.

---
This README was updated to provide a clearer quickstart, configuration guidance, and prioritized next steps for turning this scaffold into a production-ready chat backend.
