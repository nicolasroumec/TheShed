# Fase 4 (parcial) — Flujo de autenticación: guía de implementación

> Documento de handoff para implementar **registro + login** (Argon2 + JWT) en
> incrementos, cada uno con su commit. Pensado para ejecutarse en otra sesión.
> Plan completo y decisiones: ver más abajo y `docs/ARCHITECTURE.md`.

## Decisiones tomadas (resumen)
- **Argon2 en el servidor** para el hash de la contraseña maestra (coincide con
  `User.PasswordHash`). El modelo zero-knowledge (derivar clave en el cliente) queda
  como evolución futura.
- **Librería `Isopoh.Cryptography.Argon2`** → hash formato PHC con salt y parámetros
  embebidos (no hay que manejar el salt aparte).
- **Solo access token JWT** (sin refresh token todavía), firmado con HMAC-SHA256,
  clave en **User Secrets** (no en `appsettings.json`).
- **Capas:** `Security/` (cripto + JWT), `Services/` (lógica auth), `Controllers/`
  (endpoint), DTOs en `TheShed.Shared`.
- **Sin migración nueva** — los campos ya existen en `User`.

---

## ✅ Incremento 1 — Servicio de hashing Argon2 (HECHO)

> Ya implementado y en verde en el working tree. Commit pendiente de confirmar:
> `feat: add Argon2 password hashing service`

- Paquete `Isopoh.Cryptography.Argon2` agregado a `TheShed.Server.csproj`.
- `TheShed.Server/Security/IPasswordHasher.cs` — `Hash(string)` / `Verify(string, string)`.
- `TheShed.Server/Security/Argon2PasswordHasher.cs` — usa `Argon2.Hash` / `Argon2.Verify`.
- `Program.cs`: `builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();`

---

## Incremento 2 — DTOs de autenticación (Shared)

**Carpeta:** `TheShed.Shared/Models/DTOs/Auth/` · **Namespace:** `TheShed.Shared.Models.DTOs.Auth`

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

**Verificación:** `dotnet build TheShed.Shared/TheShed.Shared.csproj`
**Commit:** `feat: add authentication DTOs`

---

## Incremento 3 — Infraestructura JWT

### 3.1 Paquete
```bash
dotnet add TheShed.Server package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 3.2 Configuración
`appsettings.json` → agregar sección (sin la clave):
```json
"Jwt": {
  "Issuer": "TheShed",
  "Audience": "TheShedClient",
  "ExpiryMinutes": 60
}
```
`appsettings.Example.json` → misma sección + recordatorio de setear la clave por secrets.

Clave por **User Secrets** (no commitear):
```bash
dotnet user-secrets set "Jwt:Key" "<clave-aleatoria-larga-min-32-bytes>" --project TheShed.Server
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
        /// <summary>Genera un access token JWT para el usuario. Devuelve token y vencimiento.</summary>
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

// ... después de AddDbContext:
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

// ... en el pipeline, ANTES de app.MapControllers() y respetando el orden:
app.UseAuthentication();
app.UseAuthorization();
```
> Importante: `UseAuthentication()` debe ir **antes** de `UseAuthorization()`, y ambos
> después de `UseRouting()`.

**Verificación:** `dotnet build TheShed.Server` en verde.
**Commit:** `feat: configure JWT authentication`

---

## Incremento 4 — AuthService + AuthController

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
                return Conflict(new { message = "El email ya está registrado." });
            }
            return CreatedAtAction(nameof(Register), result.Response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        {
            var result = await _auth.LoginAsync(request, ct);
            if (!result.Success)
            {
                return Unauthorized(new { message = "Credenciales inválidas." });
            }
            return Ok(result.Response);
        }
    }
}
```

### 4.4 DI en `Program.cs`
```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
```
> `AuthService` es **Scoped** porque depende de `TheShedContext` (Scoped).
> `IPasswordHasher` y `IJwtTokenService` son Singleton (sin estado).

**Verificación:** ver sección siguiente.
**Commit:** `feat: add register and login endpoints`

---

## Incremento 5 — Docs

- `docs/TODO.md`: marcar avance de Fase 4 (auth hecho; falta AES-256, 2FA).
- `docs/ARCHITECTURE.md` (o `DECISIONS.md`): registrar las decisiones de arriba.
- **Commit:** `docs: document auth flow decisions and progress`

---

## Verificación end-to-end

```bash
dotnet ef database update --project TheShed.Server   # si la DB no existe aún
dotnet run --project TheShed.Server
```
Con el OpenAPI/REST client:
1. `POST /api/auth/register` con email nuevo → **201**; mismo email otra vez → **409**.
2. En la DB, `Users.PasswordHash` debe ser un string Argon2 (`$argon2...`), nunca la
   contraseña en claro.
3. `POST /api/auth/login` correcto → **200** + `AuthResponse` con token; password mala → **401**.
4. Pegar el token en jwt.io → verificar claims `sub`, `email`, `username`, `exp`.
5. (Opcional) endpoint dummy `[Authorize]` → **401** sin token, **200** con `Authorization: Bearer <token>`.

## Checklist de commits
- [x] `feat: add Argon2 password hashing service`  *(Inc 1 — código ya en working tree)*
- [ ] `feat: add authentication DTOs`               *(Inc 2)*
- [ ] `feat: configure JWT authentication`          *(Inc 3)*
- [ ] `feat: add register and login endpoints`      *(Inc 4)*
- [ ] `docs: document auth flow decisions and progress` *(Inc 5)*
