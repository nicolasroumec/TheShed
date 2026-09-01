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

// Stores whatever ImportAsync POSTs and serves it back through list/get, so a roundtrip test
// can check what comes out of ExportAsync against what actually went over the wire.
file class CapturingEntriesHandler : HttpMessageHandler
{
    private readonly Dictionary<int, EntryCreateRequest> _store = [];
    private int _nextId = 1;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post)
        {
            var body = (await request.Content!.ReadFromJsonAsync<EntryCreateRequest>(cancellationToken))!;
            var id = _nextId++;
            _store[id] = body;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new EntryResponse { Id = id }) };
        }

        if (request.RequestUri!.AbsolutePath == "/api/entries")
        {
            var list = _store.Select(kv => new EntryListItem { Id = kv.Key, Name = kv.Value.Name, Username = kv.Value.Username }).ToList();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(list) };
        }

        var entryId = int.Parse(request.RequestUri.AbsolutePath.Split('/').Last());
        var stored = _store[entryId];
        var response = new EntryResponse
        {
            Id = entryId,
            Name = stored.Name,
            Username = stored.Username,
            Password = stored.Password,
            Url = stored.Url,
            Notes = stored.Notes,
        };
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
    }
}

// Wraps AesEncryptionService (nonce||ciphertext||tag, same layout as the JS Web Crypto side —
// see its own doc comment) so a test can exercise real AES-GCM without a browser runtime.
file class RealAesGcmService : IAesGcmService
{
    public Task<string> EncryptAsync(byte[] key, string plaintext) => Task.FromResult(new AesEncryptionService(key).Encrypt(plaintext));
    public Task<string> DecryptAsync(byte[] key, string ciphertext) => Task.FromResult(new AesEncryptionService(key).Decrypt(ciphertext));
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

    [Fact]
    public async Task ImportAsync_OnePassword_ImportsValidRow()
    {
        var csv = "Title,Website,Username,Password,Notes\r\n" +
                   "GitHub,https://github.com,alice,hunter2,some notes\r\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var http = new HttpClient(new CapturingEntriesHandler()) { BaseAddress = new Uri("http://localhost/") };
        var client = new ImportExportClient(new EntryClient(http), new FixedVaultKeyCache(), new NoopAesGcmService());

        var result = await client.ImportAsync(stream, ImportFormat.OnePassword, vaultId: 1);

        Assert.Equal(1, result.Imported);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ImportAsync_SkipsBlankLine_WithoutAbortingSurroundingRows()
    {
        var csv = "name,username,password,url,extra\r\n" +
                   "GitHub,alice,secret,,\r\n" +
                   "\r\n" +
                   "Gmail,alice@example.com,pw2,,\r\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var http = new HttpClient(new CapturingEntriesHandler()) { BaseAddress = new Uri("http://localhost/") };
        var client = new ImportExportClient(new EntryClient(http), new FixedVaultKeyCache(), new NoopAesGcmService());

        var result = await client.ImportAsync(stream, ImportFormat.LastPass, vaultId: 1);

        Assert.Equal(2, result.Imported);
    }

    [Fact]
    public async Task ImportThenExport_RoundTripsPlaintext_ThroughRealAesGcm()
    {
        var vaultKeyCache = new FixedVaultKeyCache();
        vaultKeyCache.Set(1, System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var http = new HttpClient(new CapturingEntriesHandler()) { BaseAddress = new Uri("http://localhost/") };
        var client = new ImportExportClient(new EntryClient(http), vaultKeyCache, new RealAesGcmService());

        var csv = "name,username,password,url,extra\r\n" +
                   "GitHub,alice,hunter2,https://github.com,some notes\r\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var importResult = await client.ImportAsync(stream, ImportFormat.LastPass, vaultId: 1);
        Assert.Equal(1, importResult.Imported);

        var exportedCsv = Encoding.UTF8.GetString(await client.ExportAsync(vaultId: 1));

        Assert.Contains("GitHub,alice,hunter2,https://github.com,some notes", exportedCsv);
    }
}
