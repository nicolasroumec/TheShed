using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/auth/2fa endpoints (Sprint 18).</summary>
    public class TwoFactorClient
    {
        private readonly HttpClient _http;

        public TwoFactorClient(HttpClient http) => _http = http;

        public async Task<bool> IsEnabledAsync() =>
            (await _http.GetFromJsonAsync<TwoFactorStatusResponse>("api/auth/2fa"))!.Enabled;

        /// <summary>A fresh pending secret, or null if 2FA is already on.</summary>
        public async Task<TwoFactorSetupResponse?> BeginSetupAsync()
        {
            var response = await _http.PostAsync("api/auth/2fa/setup", null);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TwoFactorSetupResponse>()
                : null;
        }

        public async Task<bool> EnableAsync(string code) =>
            (await _http.PostAsJsonAsync("api/auth/2fa/enable", new TwoFactorCodeRequest { Code = code })).IsSuccessStatusCode;

        public async Task<bool> DisableAsync(string code) =>
            (await _http.PostAsJsonAsync("api/auth/2fa/disable", new TwoFactorCodeRequest { Code = code })).IsSuccessStatusCode;
    }
}
