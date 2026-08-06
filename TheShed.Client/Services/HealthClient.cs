using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Health;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/health endpoints.</summary>
    public class HealthClient
    {
        private readonly HttpClient _http;

        public HealthClient(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<PasswordHealthItem>> GetPasswordReportAsync() =>
            await _http.GetFromJsonAsync<List<PasswordHealthItem>>("api/health/passwords") ?? [];
    }
}
