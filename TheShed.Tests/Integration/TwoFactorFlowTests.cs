using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheShed.Server.Data;
using TheShed.Server.Security;
using TheShed.Shared.Models.DTOs.Auth;
using Xunit;

namespace TheShed.Tests.Integration
{
    /// <summary>2FA (Sprint 18) end to end: setup → enable → login second step → disable, plus
    /// replay protection. Codes are minted from the secret the server stored, at the steps the
    /// server's ±1 window accepts (each step can be used only once).</summary>
    public class TwoFactorFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string Password = "IntegrationTest123!";
        private readonly CustomWebApplicationFactory _factory;

        public TwoFactorFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task FullFlow_EnableLoginDisable()
        {
            var client = _factory.CreateAuthenticatedClient();
            var (email, _) = await client.RegisterNewUserAsync(Password);
            var step = await CurrentStepAwayFromBoundaryAsync();

            var setup = await client.PostJsonWithCsrfAsync("api/auth/2fa/setup", new { });
            setup.EnsureSuccessStatusCode();
            var body = await setup.Content.ReadFromJsonAsync<TwoFactorSetupResponse>();
            Assert.StartsWith("otpauth://totp/The%20Shed:", body!.OtpAuthUri);
            Assert.Contains($"secret={body.Secret}", body.OtpAuthUri);

            var secret = await StoredSecretAsync(email);
            Assert.False((await client.GetFromJsonAsync<TwoFactorStatusResponse>("api/auth/2fa"))!.Enabled);

            (await client.PostJsonWithCsrfAsync("api/auth/2fa/enable", Code(secret, step - 1))).EnsureSuccessStatusCode();
            Assert.True((await client.GetFromJsonAsync<TwoFactorStatusResponse>("api/auth/2fa"))!.Enabled);

            // Password alone is no longer enough.
            var login = _factory.CreateAuthenticatedClient();
            var noCode = await login.PostJsonWithCsrfAsync("api/auth/login", new { Email = email, Password });
            Assert.Equal(HttpStatusCode.Unauthorized, noCode.StatusCode);
            Assert.Contains("twoFactorRequired", await noCode.Content.ReadAsStringAsync());

            var wrongCode = await login.PostJsonWithCsrfAsync("api/auth/login",
                new { Email = email, Password, TotpCode = WrongCode(secret, step) });
            Assert.Equal(HttpStatusCode.Unauthorized, wrongCode.StatusCode);

            var withCode = await login.PostJsonWithCsrfAsync("api/auth/login",
                new { Email = email, Password, TotpCode = Totp.Code(secret, step) });
            withCode.EnsureSuccessStatusCode();

            // Replay: the same code (or an older one) is rejected even though it's still in the window.
            var replay = await _factory.CreateAuthenticatedClient().PostJsonWithCsrfAsync("api/auth/login",
                new { Email = email, Password, TotpCode = Totp.Code(secret, step) });
            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

            (await client.PostJsonWithCsrfAsync("api/auth/2fa/disable", Code(secret, step + 1))).EnsureSuccessStatusCode();
            Assert.False((await client.GetFromJsonAsync<TwoFactorStatusResponse>("api/auth/2fa"))!.Enabled);
            Assert.Null(await StoredSecretOrNullAsync(email));

            var plainLogin = await _factory.CreateAuthenticatedClient().PostJsonWithCsrfAsync("api/auth/login",
                new { Email = email, Password });
            plainLogin.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Enable_WrongCode_KeepsTwoFactorOff()
        {
            var client = _factory.CreateAuthenticatedClient();
            var (email, _) = await client.RegisterNewUserAsync(Password);
            var step = await CurrentStepAwayFromBoundaryAsync();
            (await client.PostJsonWithCsrfAsync("api/auth/2fa/setup", new { })).EnsureSuccessStatusCode();
            var secret = await StoredSecretAsync(email);

            var response = await client.PostJsonWithCsrfAsync("api/auth/2fa/enable",
                new TwoFactorCodeRequest { Code = WrongCode(secret, step) });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False((await client.GetFromJsonAsync<TwoFactorStatusResponse>("api/auth/2fa"))!.Enabled);
        }

        [Fact]
        public async Task Setup_WhenAlreadyEnabled_Returns409()
        {
            var client = _factory.CreateAuthenticatedClient();
            var (email, _) = await client.RegisterNewUserAsync(Password);
            var step = await CurrentStepAwayFromBoundaryAsync();
            (await client.PostJsonWithCsrfAsync("api/auth/2fa/setup", new { })).EnsureSuccessStatusCode();
            var secret = await StoredSecretAsync(email);
            (await client.PostJsonWithCsrfAsync("api/auth/2fa/enable", Code(secret, step))).EnsureSuccessStatusCode();

            // Otherwise a stolen session could swap in an attacker-controlled secret.
            var again = await client.PostJsonWithCsrfAsync("api/auth/2fa/setup", new { });

            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        }

        [Fact]
        public async Task Endpoints_RequireAuthentication()
        {
            var anonymous = _factory.CreateAuthenticatedClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("api/auth/2fa")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anonymous.PostJsonWithCsrfAsync("api/auth/2fa/setup", new { })).StatusCode);
        }

        private static TwoFactorCodeRequest Code(byte[] secret, long step) => new() { Code = Totp.Code(secret, step) };

        // A well-formed code that matches no step in the ±1 window.
        private static string WrongCode(byte[] secret, long step)
        {
            var valid = new[] { step - 1, step, step + 1 }.Select(s => Totp.Code(secret, s)).ToHashSet();
            return Enumerable.Range(0, 4).Select(i => i.ToString("D6")).First(c => !valid.Contains(c));
        }

        // The test mints codes for step-1..step+1; if a step boundary passes mid-test, step-1
        // falls out of the server's window. Waiting out the last few seconds of a step avoids it.
        private static async Task<long> CurrentStepAwayFromBoundaryAsync()
        {
            var secondsIntoStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % Totp.StepSeconds;
            if (secondsIntoStep >= Totp.StepSeconds - 5)
            {
                await Task.Delay(TimeSpan.FromSeconds(Totp.StepSeconds - secondsIntoStep + 1));
            }
            return Totp.StepAt(DateTimeOffset.UtcNow);
        }

        private async Task<byte[]> StoredSecretAsync(string email) =>
            Convert.FromBase64String((await StoredSecretOrNullAsync(email))!);

        private async Task<string?> StoredSecretOrNullAsync(string email)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TheShedContext>();
            return await db.Users.Where(u => u.Email == email).Select(u => u.TwoFactorSecret).SingleAsync();
        }
    }
}
