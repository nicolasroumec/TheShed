# Phase 4 (partial) — Authentication flow: implementation guide

> **Historical document.** The handoff used to implement **register + login** (Argon2 + JWT)
> in increments, one commit each, meant to run in another session. The code below is the
> original plan: the flow has since moved to an `HttpOnly` cookie (D6), client-side key
> derivation (D7) and an auth hash instead of the master password (D11) — see `DECISIONS.md`.

## Decisions taken (summary)
- **Argon2 on the server** for the master password hash (matches
  `User.PasswordHash`). The zero-knowledge model (deriving the key on the client) is left
  as a future evolution.
- **`Isopoh.Cryptography.Argon2` library** → PHC-format hash with embedded salt and
  parameters (no need to handle the salt separately).
- **JWT access token only** (no refresh token yet), signed with HMAC-SHA256, key in
  **User Secrets** (not in `appsettings.json`).
- **Layers:** `Security/` (crypto + JWT), `Services/` (auth logic), `Controllers/`
  (endpoint), DTOs in `TheShed.Shared`.
- **No new migration** — the fields already exist on `User`.

---

## ✅ Increment 1 — Argon2 hashing service (DONE)

> Already implemented and green in the working tree. Commit awaiting confirmation:
> `feat: add Argon2 password hashing service`

- `Isopoh.Cryptography.Argon2` package added to `TheShed.Server.csproj`.
- `TheShed.Server/Security/IPasswordHasher.cs` — `Hash(string)` / `Verify(string, string)`.
- `TheShed.Server/Security/Argon2PasswordHasher.cs` — uses `Argon2.Hash` / `Argon2.Verify`.
- `Program.cs`: `builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();`

---

## Increment 2 — Authentication DTOs (Shared)

**Folder:** `TheShed.Shared/Models/DTOs/Auth/` · **Namespace:** `TheShed.Shared.Models.DTOs.Auth`

`RegisterRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required, MinLength(3), MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}
```

`LoginRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Auth
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
```

`AuthResponse.cs`
```csharp
namespace TheShed.Shared.Models.DTOs.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
```

**Verification:** `dotnet build TheShed.Shared/TheShed.Shared.csproj`
**Commit:** `feat: add authentication DTOs`

---

## Increment 3 — JWT infrastructure

### 3.1 Package
```bash
dotnet add TheShed.Server package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 3.2 Configuration
`appsettings.json` → add the section (without the key):
```json
"Jwt": {
  "Issuer": "TheShed",
  "Audience": "TheShedClient",
  "ExpiryMinutes": 60
}
```
`appsettings.Example.json` → same section + a reminder to set the key through secrets.

Key through **User Secrets** (do not commit it):
```bash
dotnet user-secrets set "Jwt:Key" "<long-random-key-min-32-bytes>" --project TheShed.Server
```

### 3.3 `TheShed.Server/Security/JwtSettings.cs`
```csharp
namespace TheShed.Server.Security
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;
    }
}
```

### 3.4 `TheShed.Server/Security/IJwtTokenService.cs`
```csharp
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Security
{
    public interface IJwtTokenService
    {
        /// <summary>Generates a JWT access token for the user. Returns the token and its expiry.</summary>
        (string Token, DateTime ExpiresAt) GenerateToken(User user);
    }
}
```

### 3.5 `TheShed.Server/Security/JwtTokenService.cs`
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Security
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

        public (string Token, DateTime ExpiresAt) GenerateToken(User user)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
```

### 3.6 `Program.cs`
```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TheShed.Server.Security;

// ... after AddDbContext:
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });
builder.Services.AddAuthorization();

// ... in the pipeline, BEFORE app.MapControllers() and keeping this order:
app.UseAuthentication();
app.UseAuthorization();
```
> Important: `UseAuthentication()` must come **before** `UseAuthorization()`, and both
> after `UseRouting()`.

**Verification:** `dotnet build TheShed.Server` green.
**Commit:** `feat: configure JWT authentication`

---

## Increment 4 — AuthService + AuthController

### 4.1 `TheShed.Server/Services/IAuthService.cs`
```csharp
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Server.Services
{
    public enum AuthError { None, EmailInUse, InvalidCredentials }

    public record AuthResult(bool Success, AuthError Error, AuthResponse? Response);

    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    }
}
```

### 4.2 `TheShed.Server/Services/AuthService.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Security;
using TheShed.Shared.Models.DTOs.Auth;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly TheShedContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtTokenService _jwt;

        public AuthService(TheShedContext db, IPasswordHasher hasher, IJwtTokenService jwt)
        {
            _db = db;
            _hasher = hasher;
            _jwt = jwt;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            {
                return new AuthResult(false, AuthError.EmailInUse, null);
            }

            var user = new User
            {
                Username = request.Username.Trim(),
                Email = email,
                PasswordHash = _hasher.Hash(request.Password)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            return Success(user);
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

            if (user is null || !user.IsActive || !_hasher.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResult(false, AuthError.InvalidCredentials, null);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Success(user);
        }

        private AuthResult Success(User user)
        {
            var (token, expiresAt) = _jwt.GenerateToken(user);
            return new AuthResult(true, AuthError.None, new AuthResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                Username = user.Username,
                Email = user.Email
            });
        }
    }
}
```

### 4.3 `TheShed.Server/Controllers/AuthController.cs`
```csharp
using Microsoft.AspNetCore.Mvc;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
        {
            var result = await _auth.RegisterAsync(request, ct);
            if (!result.Success)
            {
                return Conflict(new { message = "That email is already registered." });
            }
            return CreatedAtAction(nameof(Register), result.Response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        {
            var result = await _auth.LoginAsync(request, ct);
            if (!result.Success)
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }
            return Ok(result.Response);
        }
    }
}
```

### 4.4 DI in `Program.cs`
```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
```
> `AuthService` is **Scoped** because it depends on `TheShedContext` (Scoped).
> `IPasswordHasher` and `IJwtTokenService` are Singleton (stateless).

**Verification:** see the next section.
**Commit:** `feat: add register and login endpoints`

---

## Increment 5 — Docs

- `docs/TODO.md`: mark Phase 4 progress (auth done; AES-256 and 2FA missing).
- `docs/ARCHITECTURE.md` (or `DECISIONS.md`): record the decisions above.
- **Commit:** `docs: document auth flow decisions and progress`

---

## End-to-end verification

```bash
dotnet ef database update --project TheShed.Server   # if the DB doesn't exist yet
dotnet run --project TheShed.Server
```
With the OpenAPI/REST client:
1. `POST /api/auth/register` with a new email → **201**; the same email again → **409**.
2. In the DB, `Users.PasswordHash` must be an Argon2 string (`$argon2...`), never the
   plaintext password.
3. A correct `POST /api/auth/login` → **200** + `AuthResponse` with a token; wrong password → **401**.
4. Paste the token into jwt.io → check the `sub`, `email`, `username`, `exp` claims.
5. (Optional) a dummy `[Authorize]` endpoint → **401** without a token, **200** with `Authorization: Bearer <token>`.

## Commit checklist
- [x] `feat: add Argon2 password hashing service`  *(Inc 1 — code already in the working tree)*
- [ ] `feat: add authentication DTOs`               *(Inc 2)*
- [ ] `feat: configure JWT authentication`          *(Inc 3)*
- [ ] `feat: add register and login endpoints`      *(Inc 4)*
- [ ] `docs: document auth flow decisions and progress` *(Inc 5)*
