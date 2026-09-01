using System.Text;
using TheShed.Client.Services;
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
}
