using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using TheShed.Shared.Models.DTOs.Attachments;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/entries/{entryId}/attachments endpoints.</summary>
    public class AttachmentClient
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // mirrors AttachmentSettings.MaxFileSizeBytes

        private readonly HttpClient _http;

        public AttachmentClient(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<AttachmentResponse>> ListAsync(int entryId) =>
            await _http.GetFromJsonAsync<List<AttachmentResponse>>($"api/entries/{entryId}/attachments") ?? [];

        public async Task<HttpResponseMessage> UploadAsync(int entryId, IBrowserFile file)
        {
            using var content = new MultipartFormDataContent();
            using var stream = new StreamContent(file.OpenReadStream(MaxFileSizeBytes));
            content.Add(stream, "file", file.Name);
            return await _http.PostAsync($"api/entries/{entryId}/attachments", content);
        }

        public async Task<(string FileName, byte[] Content)> DownloadAsync(int entryId, int attachmentId)
        {
            var response = await _http.GetAsync($"api/entries/{entryId}/attachments/{attachmentId}/download");
            response.EnsureSuccessStatusCode();
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? "download";
            return (fileName.Trim('"'), await response.Content.ReadAsByteArrayAsync());
        }

        public async Task DeleteAsync(int entryId, int attachmentId) =>
            (await _http.DeleteAsync($"api/entries/{entryId}/attachments/{attachmentId}")).EnsureSuccessStatusCode();
    }
}
