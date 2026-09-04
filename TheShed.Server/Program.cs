using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
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

// Security — antiforgery (A4), second line of defense on top of the cookie's SameSite=Lax
// (D6). The client fetches a token from AntiforgeryController and echoes it back in this
// header on every mutating request; the middleware below validates it against the
// antiforgery cookie ASP.NET Core sets alongside it.
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

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

// Application services — user lookups (public key by email, Sprint 27; Scoped: depends on TheShedContext)
builder.Services.AddScoped<IUserService, UserService>();

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

// Security — HSTS is production-only: in development it would pin localhost to https in the
// browser's preload cache and outlive the debugging session.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Security — response headers. Early in the pipeline so static files carry them too.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY"; // frame-ancestors below covers this too, for older browsers
    headers["Referrer-Policy"] = "no-referrer";
    // 'wasm-unsafe-eval' is what the Blazor WebAssembly runtime needs to compile its modules;
    // script-src stays free of 'unsafe-inline'/'unsafe-eval', which is the part that stops XSS.
    // The CDN entries are Bootstrap Icons (jsdelivr) and the Bunny Fonts stylesheet + font files.
    // ponytail: style-src still needs 'unsafe-inline' because 8 components use style="" attributes;
    // move those to classes to drop it.
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "img-src 'self' data:; " +
        "script-src 'self' 'wasm-unsafe-eval'; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.bunny.net; " +
        "font-src 'self' https://cdn.jsdelivr.net https://fonts.bunny.net; " +
        "connect-src 'self'";
    await next();
});

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

// After UseRouting: endpoint-specific policies need the endpoint already resolved.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Security — antiforgery validation (A4). GET/HEAD/OPTIONS/TRACE are read-only and exempt;
// every other verb must carry a valid X-CSRF-TOKEN header or gets rejected before it reaches
// a controller. AntiforgeryController's token endpoint is itself a GET, so it's never blocked.
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method) &&
        !HttpMethods.IsOptions(method) && !HttpMethods.IsTrace(method))
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
    }
    await next();
});

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
