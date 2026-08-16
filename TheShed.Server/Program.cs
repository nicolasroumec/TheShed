using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TheShed.Server.Data;
using TheShed.Server.Security;
using TheShed.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQL Server
builder.Services.AddDbContext<TheShedContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Seguridad — hashing de la contraseña maestra (Argon2)
builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

// Seguridad — cifrado de entradas (AES-256-GCM)
builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("Encryption"));
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();

// Seguridad — autenticación JWT (access token, HMAC-SHA256)
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
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Token) && ctx.Request.Cookies.TryGetValue("authToken", out var t))
                {
                    ctx.Token = t;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Security — rate limiting on the auth endpoints. Argon2 is deliberately expensive, so an
// unthrottled login is both a brute-force surface against the master password and a way to
// burn server CPU. Applied via [EnableRateLimiting] on AuthController.
var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
var authWindowMinutes = builder.Configuration.GetValue("RateLimiting:AuthWindowMinutes", 5);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // ponytail: partitioned by IP only. Model binding runs after this middleware, so the email
    // is not available here — brute force spread across many IPs still gets through. Per-email
    // throttling needs a counter inside AuthService; add it if IP limiting proves insufficient.
    // Behind a reverse proxy this needs UseForwardedHeaders to see the real client IP.
    options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromMinutes(authWindowMinutes)
            }));
});

// Servicios de aplicación — autenticación (Scoped: depende de TheShedContext)
builder.Services.AddScoped<IAuthService, AuthService>();

// Application services — vault access (Scoped: depends on TheShedContext)
builder.Services.AddScoped<IVaultAccessService, VaultAccessService>();

// Application services — vaults (Scoped: depends on TheShedContext)
builder.Services.AddScoped<IVaultService, VaultService>();

// Application services — password entries (Scoped: depends on TheShedContext)
builder.Services.AddScoped<IPasswordEntryService, PasswordEntryService>();
builder.Services.AddScoped<ISecureNoteService, SecureNoteService>();
builder.Services.AddScoped<ITagService, TagService>();

// Application services — trash (Scoped: depends on TheShedContext)
builder.Services.Configure<TrashSettings>(builder.Configuration.GetSection("Trash"));
builder.Services.AddScoped<ITrashService, TrashService>();
builder.Services.AddHostedService<TrashPurgeService>();

// Application services — attachments (local filesystem storage; Scoped: depends on TheShedContext)
builder.Services.Configure<AttachmentSettings>(builder.Configuration.GetSection("Attachments"));
builder.Services.AddSingleton<IAttachmentStorage, LocalFileAttachmentStorage>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

// Application services — password health (Scoped: depends on TheShedContext)
builder.Services.AddScoped<IPasswordHealthService, PasswordHealthService>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

// After UseRouting: endpoint-specific policies need the endpoint already resolved.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
