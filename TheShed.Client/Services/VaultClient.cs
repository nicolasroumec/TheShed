using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Vaults;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/vaults endpoints. The HttpClient already carries
    /// the Bearer token (set by JwtAuthenticationStateProvider).</summary>
    public class VaultClient
    {
        private readonly HttpClient _http;

        public VaultClient(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<VaultListItem>> ListAsync() =>
            await _http.GetFromJsonAsync<List<VaultListItem>>("api/vaults") ?? [];

        public Task<VaultResponse?> GetAsync(int id) =>
            _http.GetFromJsonAsync<VaultResponse>($"api/vaults/{id}");

        public async Task<VaultResponse?> CreateAsync(VaultCreateRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/vaults", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VaultResponse>();
        }
    }
}
