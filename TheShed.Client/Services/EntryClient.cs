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

        /// <summary>Lists a vault's entries, optionally filtered by tag. Name/Username/Url come
        /// back as ciphertext (Sprint 26) — there's no search param because the server can't
        /// match against it; the caller decrypts and filters client-side.</summary>
        public async Task<IReadOnlyList<EntryListItem>> ListAsync(
            int vaultId, int? tagId = null, CancellationToken cancellationToken = default)
        {
            var url = $"api/entries?vaultId={vaultId}";
            if (tagId is not null)
            {
                url += $"&tagId={tagId}";
            }
            return await _http.GetFromJsonAsync<List<EntryListItem>>(url, cancellationToken) ?? [];
        }

        public Task<EntryResponse?> GetAsync(int entryId) =>
            _http.GetFromJsonAsync<EntryResponse>($"api/entries/{entryId}");

        public async Task<EntryResponse?> CreateAsync(EntryCreateRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/entries", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EntryResponse>();
        }

        public async Task<EntryResponse?> UpdateAsync(int entryId, EntryUpdateRequest request)
        {
            var response = await _http.PutAsJsonAsync($"api/entries/{entryId}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EntryResponse>();
        }

        public async Task DeleteAsync(int entryId) =>
            (await _http.DeleteAsync($"api/entries/{entryId}")).EnsureSuccessStatusCode();

        public async Task AddTagAsync(int entryId, int tagId) =>
            (await _http.PutAsync($"api/entries/{entryId}/tags/{tagId}", null)).EnsureSuccessStatusCode();

        public async Task RemoveTagAsync(int entryId, int tagId) =>
            (await _http.DeleteAsync($"api/entries/{entryId}/tags/{tagId}")).EnsureSuccessStatusCode();

        public async Task SetFavoriteAsync(int entryId, bool isFavorite)
        {
            var response = isFavorite
                ? await _http.PutAsync($"api/entries/{entryId}/favorite", null)
                : await _http.DeleteAsync($"api/entries/{entryId}/favorite");
            response.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyList<EntryHistoryItem>> GetHistoryAsync(int entryId) =>
            await _http.GetFromJsonAsync<List<EntryHistoryItem>>($"api/entries/{entryId}/history") ?? [];

        public Task<EntryHistoryDetail?> GetHistoryEntryAsync(int entryId, int historyId) =>
            _http.GetFromJsonAsync<EntryHistoryDetail>($"api/entries/{entryId}/history/{historyId}");
    }
}
