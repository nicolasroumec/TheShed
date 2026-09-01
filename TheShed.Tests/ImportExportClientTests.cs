using System.Net;
using System.Net.Http.Json;
using System.Text;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Security;

namespace TheShed.Tests;

file class NoopAesGcmService : IAesGcmService
{
    public Task<string> EncryptAsync(byte[] key, string plaintext) => Task.FromResult("enc:" + plaintext);
    public Task<string> DecryptAsync(byte[] key, string ciphertext) => Task.FromResult(ciphertext);
}

file class FixedVaultKeyCache : IVaultKeyCache
{
    private byte[]? _key = new byte[32];
    public void Set(int vaultId, byte[] key) => _key = key;
    public byte[]? Get(int vaultId) => _key;
    public void Clear() => _key = null;
}

// Routes GET api/entries?vaultId=.. to the list and GET api/entries/{id} to the detail — just
// enough to drive ExportAsync's list-then-get-each fetch without a real server.
file class FakeEntriesHandler : HttpMessageHandler
{
    private readonly List<EntryListItem> _list;
    private readonly Dictionary<int, EntryResponse> _byId;

    public FakeEntriesHandler(List<EntryListItem> list, Dictionary<int, EntryResponse> byId)
    {
        _list = list;
        _byId = byId;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path == "/api/entries")
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(_list) });
        }

        var id = int.Parse(path.Split('/').Last());
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(_byId[id]) });
    }
}

// Only exercises rows that fail before EntryClient.CreateAsync is reached, so no HTTP call is
// ever made and the HttpClient below never needs a real base address to succeed against.
public class ImportExportClientTests
{
    private static ImportExportClient CreateClient() =>
        new(new EntryClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }),
            new FixedVaultKeyCache(), new NoopAesGcmService());

    [Fact]
    public async Task ImportAsync_SkipsRowMissingRequiredField_WithoutAborting()
    {
        var csv = "name,username,password,url,extra\r\n" +
                   "GitHub,,secret,https://github.com,\r\n"; // missing username
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await CreateClient().ImportAsync(stream, ImportFormat.LastPass, vaultId: 1);

        Assert.Equal(0, result.Imported);
        var error = Assert.Single(result.Errors);
        Assert.Equal(2, error.Row);
    }

    [Fact]
    public async Task ImportAsync_RecordsError_WhenFormatColumnsDontMatchFile()
    {
        // A 1Password-shaped file imported as Bitwarden: none of Bitwarden's columns exist.
        var csv = "Title,Website,Username,Password,Notes\r\n" +
                   "GitHub,https://github.com,me,secret,\r\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await CreateClient().ImportAsync(stream, ImportFormat.Bitwarden, vaultId: 1);

        Assert.Equal(0, result.Imported);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task ExportAsync_DecryptsEveryField_AndWritesOneCsvRowPerEntry()
    {
        var list = new List<EntryListItem> { new() { Id = 5, Name = "GitHub", Username = "me" } };
        var byId = new Dictionary<int, EntryResponse>
        {
            [5] = new()
            {
                Id = 5,
                Name = "GitHub",
                Username = "me",
                Password = "secret",
                Url = "https://github.com",
                Notes = null,
            },
        };
        var http = new HttpClient(new FakeEntriesHandler(list, byId)) { BaseAddress = new Uri("http://localhost/") };
        var client = new ImportExportClient(new EntryClient(http), new FixedVaultKeyCache(), new NoopAesGcmService());

        var csvBytes = await client.ExportAsync(vaultId: 1);
        var csv = Encoding.UTF8.GetString(csvBytes);

        Assert.Contains("Name,Username,Password,Url,Notes", csv);
        Assert.Contains("GitHub,me,secret,https://github.com,", csv);
    }
}
