using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TheShed.Server.Data;

namespace TheShed.Tests.Integration
{
    /// <summary>Boots the real ASP.NET Core pipeline (Program.cs, middleware order, auth,
    /// antiforgery) against an isolated SQLite in-memory database instead of the real SQL
    /// Server — real relational behavior (constraints, transactions), zero external
    /// dependencies. One instance per test class via IClassFixture; tests use unique data
    /// (email, vault names) instead of resetting the database between tests.</summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        // Keeps the SQLite in-memory database alive for the factory's lifetime — the
        // database is destroyed the moment its one and only connection closes.
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        public CustomWebApplicationFactory() => _connection.Open();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Jwt:Key only exists in dev via user-secrets (see appsettings.Example.json) —
            // unusable on another machine or in CI. RateLimiting:AuthPermitLimit's real
            // default (10/5min) would have the auth tests in this same run throttle each
            // other. Program.cs reads both eagerly, before Build() — UseSetting is what
            // actually reaches that read (unlike ConfigureAppConfiguration, which lands too
            // late for values Program.cs consumes at the top level).
            builder.UseSetting("Jwt:Key", "integration-test-signing-key-at-least-32-bytes-long");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
            builder.UseSetting("Attachments:StoragePath",
                Path.Combine(Path.GetTempPath(), "TheShedTests", Guid.NewGuid().ToString("N")));

            builder.ConfigureServices(services =>
            {
                // Removing just DbContextOptions<TheShedContext> isn't enough: AddDbContext
                // also registers an IDbContextOptionsConfiguration<TheShedContext> per call,
                // and those stack rather than replace — without this second removal, both
                // Program.cs's UseSqlServer and this UseSqlite run against the same options,
                // and EF Core refuses to start with two providers configured at once.
                services.RemoveAll<DbContextOptions<TheShedContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<TheShedContext>>();

                services.AddDbContext<TheShedContext>(options => options.UseSqlite(_connection));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<TheShedContext>().Database.EnsureCreated();

            return host;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
