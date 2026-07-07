using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Notes;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/notes endpoints. Listings carry metadata only;
    /// the decrypted content is fetched one note at a time via <see cref="GetAsync"/>.</summary>
    public class NoteClient
    {
        private readonly HttpClient _http;

        public NoteClient(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<NoteListItem>> ListAsync(int vaultId) =>
            await _http.GetFromJsonAsync<List<NoteListItem>>($"api/notes?vaultId={vaultId}") ?? [];

        public Task<NoteResponse?> GetAsync(int noteId) =>
            _http.GetFromJsonAsync<NoteResponse>($"api/notes/{noteId}");

        public async Task<NoteResponse?> CreateAsync(NoteCreateRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/notes", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<NoteResponse>();
        }

        public async Task<NoteResponse?> UpdateAsync(int noteId, NoteUpdateRequest request)
        {
            var response = await _http.PutAsJsonAsync($"api/notes/{noteId}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<NoteResponse>();
        }

        public async Task DeleteAsync(int noteId) =>
            (await _http.DeleteAsync($"api/notes/{noteId}")).EnsureSuccessStatusCode();
    }
}
