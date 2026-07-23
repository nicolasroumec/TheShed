using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Tags;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/tags endpoints. Tags are per-user.</summary>
    // ponytail: no rename here; the API supports PUT but the UI only creates/deletes for now.
    public class TagClient
    {
        private readonly HttpClient _http;

        public TagClient(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<TagResponse>> ListAsync() =>
            await _http.GetFromJsonAsync<List<TagResponse>>("api/tags") ?? [];

        public async Task<TagResponse?> CreateAsync(TagCreateRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/tags", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TagResponse>();
        }

        public async Task DeleteAsync(int tagId) =>
            (await _http.DeleteAsync($"api/tags/{tagId}")).EnsureSuccessStatusCode();
    }
}
