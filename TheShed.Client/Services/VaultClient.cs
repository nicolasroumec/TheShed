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

        // --- Members ---

        public async Task<IReadOnlyList<VaultMemberItem>> ListMembersAsync(int vaultId) =>
            await _http.GetFromJsonAsync<List<VaultMemberItem>>($"api/vaults/{vaultId}/members") ?? [];

        /// <summary>Shares the vault with a user by email. Returns null on success, or the
        /// server's error message (unknown email / already a member) to show the owner.</summary>
        public async Task<string?> AddMemberAsync(int vaultId, VaultMemberAddRequest request)
        {
            var response = await _http.PostAsJsonAsync($"api/vaults/{vaultId}/members", request);
            return response.IsSuccessStatusCode ? null : await response.Content.ReadAsStringAsync();
        }

        public async Task UpdateMemberRoleAsync(int vaultId, int memberUserId, VaultMemberRoleUpdateRequest request)
        {
            var response = await _http.PutAsJsonAsync($"api/vaults/{vaultId}/members/{memberUserId}", request);
            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveMemberAsync(int vaultId, int memberUserId)
        {
            var response = await _http.DeleteAsync($"api/vaults/{vaultId}/members/{memberUserId}");
            response.EnsureSuccessStatusCode();
        }
    }
}
