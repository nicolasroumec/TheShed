using System.Net;
using Xunit;

namespace TheShed.Tests.Integration
{
    /// <summary>Proves the test host itself boots correctly (SQLite database created, Jwt
    /// override picked up, real middleware pipeline running) before anything in this folder
    /// tries to test actual behavior on top of it.</summary>
    public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public SmokeTests(CustomWebApplicationFactory factory) => _client = factory.CreateAuthenticatedClient();

        [Fact]
        public async Task AntiforgeryToken_DevuelveUnTokenNoVacio()
        {
            var response = await _client.GetAsync("api/antiforgery/token");

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("token", body);
        }

        [Fact]
        public async Task Me_SinCookie_Devuelve401()
        {
            var response = await _client.GetAsync("api/auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
