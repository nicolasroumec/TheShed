using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Users;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/users endpoints.</summary>
    public class UserClient
    {
        private readonly HttpClient _http;

        public UserClient(HttpClient http) => _http = http;

        /// <summary>Looks up a user's RSA public key by email, to wrap a vault key for them
        /// when sharing (Sprint 27). Null if no user with that email, or one with no keypair yet.</summary>
        public async Task<UserPublicKeyResponse?> GetPublicKeyAsync(string email)
        {
            var response = await _http.GetAsync($"api/users/public-key?email={Uri.EscapeDataString(email)}");
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<UserPublicKeyResponse>()
                : null;
        }
    }
}
