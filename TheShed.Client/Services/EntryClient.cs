using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Entries;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/entries endpoints. Listings carry metadata only;
    /// the decrypted password is fetched one entry at a time via <see cref="GetAsync"/>.</summary>
    public class EntryClient
    {
        private readonly HttpClient _http;

        public EntryClient(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<EntryListItem>> ListAsync(int vaultId) =>
            await _http.GetFromJsonAsync<List<EntryListItem>>($"api/entries?vaultId={vaultId}") ?? [];

        public Task<EntryResponse?> GetAsync(int entryId) =>
            _http.GetFromJsonAsync<EntryResponse>($"api/entries/{entryId}");
    }
}
