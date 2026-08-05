using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Trash;
using TheShed.Shared.Models.Enums;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/trash endpoints.</summary>
    public class TrashClient
    {
        private readonly HttpClient _http;

        public TrashClient(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<TrashItem>> ListAsync() =>
            await _http.GetFromJsonAsync<List<TrashItem>>("api/trash") ?? [];

        public async Task RestoreAsync(TrashItemType type, int id) =>
            (await _http.PostAsync($"api/trash/{type}/{id}/restore", null)).EnsureSuccessStatusCode();

        public async Task PurgeAsync(TrashItemType type, int id) =>
            (await _http.DeleteAsync($"api/trash/{type}/{id}/purge")).EnsureSuccessStatusCode();
    }
}
