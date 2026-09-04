using System.Net;
using Xunit;

namespace TheShed.Tests.Integration
{
    /// <summary>Verifies the Increment 2 helper itself — that registering through it actually
    /// leaves the client authenticated against the real pipeline — before Increment 3 builds a
    /// full auth-flow suite on top of it.</summary>
    public class AuthTestHelperTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthTestHelperTests(CustomWebApplicationFactory factory) => _client = factory.CreateAuthenticatedClient();

        [Fact]
        public async Task RegisterNewUserAsync_LeavesTheClientAuthenticated()
        {
            var (email, username) = await _client.RegisterNewUserAsync();

            var me = await _client.GetAsync("api/auth/me");

            me.EnsureSuccessStatusCode();
            var body = await me.Content.ReadAsStringAsync();
            Assert.Contains(username, body);
            Assert.Contains(email, body);
        }

        [Fact]
        public async Task PostJsonWithCsrfAsync_ReusingTheSameEmail_Returns409()
        {
            var (email, _) = await _client.RegisterNewUserAsync();

            var duplicate = await _client.PostJsonWithCsrfAsync("api/auth/register", new
            {
                Username = "someoneelse",
                Email = email,
                Password = "IntegrationTest123!",
                KeySalt = "c2FsdA==",
                PublicKey = "pem",
                EncryptedPrivateKey = "blob"
            });

            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        }
    }
}
