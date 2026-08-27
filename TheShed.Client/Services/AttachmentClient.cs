using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using TheShed.Shared.Models.DTOs.Attachments;
using TheShed.Shared.Security;

namespace TheShed.Client.Services
{
    /// <summary>Typed wrapper over the /api/entries/{entryId}/attachments endpoints. Upload and
    /// download encrypt/decrypt the file client-side with the vault key (Sprint 27, same move
    /// Sprint 26 made for entry/note fields) — reuses <see cref="IAesGcmService"/>'s string API
    /// by base64-encoding the raw bytes first, rather than adding byte-array-specific interop.</summary>
    public class AttachmentClient
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // mirrors AttachmentSettings.MaxFileSizeBytes

        private readonly HttpClient _http;
        private readonly IAesGcmService _aesGcm;

        public AttachmentClient(HttpClient http, IAesGcmService aesGcm)
        {
            _http = http;
            _aesGcm = aesGcm;
        }

        public async Task<IReadOnlyList<AttachmentResponse>> ListAsync(int entryId) =>
            await _http.GetFromJsonAsync<List<AttachmentResponse>>($"api/entries/{entryId}/attachments") ?? [];

        public async Task<HttpResponseMessage> UploadAsync(int entryId, IBrowserFile file, byte[] vaultKey)
        {
            using var stream = file.OpenReadStream(MaxFileSizeBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var ciphertext = await _aesGcm.EncryptAsync(vaultKey, Convert.ToBase64String(buffer.ToArray()));

            using var content = new MultipartFormDataContent();
            using var byteContent = new ByteArrayContent(Encoding.UTF8.GetBytes(ciphertext));
            content.Add(byteContent, "file", file.Name);
            return await _http.PostAsync($"api/entries/{entryId}/attachments", content);
        }

        public async Task<(string FileName, byte[] Content)> DownloadAsync(int entryId, int attachmentId, byte[] vaultKey)
        {
            var response = await _http.GetAsync($"api/entries/{entryId}/attachments/{attachmentId}/download");
            response.EnsureSuccessStatusCode();
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? "download";
            var ciphertext = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
            var plaintextBase64 = await _aesGcm.DecryptAsync(vaultKey, ciphertext);
            return (fileName.Trim('"'), Convert.FromBase64String(plaintextBase64));
        }

        public async Task DeleteAsync(int entryId, int attachmentId) =>
            (await _http.DeleteAsync($"api/entries/{entryId}/attachments/{attachmentId}")).EnsureSuccessStatusCode();
    }
}
