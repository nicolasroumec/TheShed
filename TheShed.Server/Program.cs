using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Security;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQL Server
builder.Services.AddDbContext<TheShedContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Seguridad — hashing de la contraseña maestra (Argon2)
builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add CORS for WebAssembly client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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

app.UseCors("AllowAll");
app.UseRouting();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
