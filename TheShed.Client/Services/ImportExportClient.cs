using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Security;

namespace TheShed.Client.Services
{
    public enum ImportFormat { LastPass, Bitwarden, OnePassword }

    /// <summary>Client-side CSV import for entries (Sprint 17). Parses with CsvHelper, encrypts
    /// each field with the vault key — same path as EntriesPanel's SubmitAsync — and uploads the
    /// ciphertext via <see cref="EntryClient.CreateAsync"/>. The server never sees plaintext.</summary>
    public class ImportExportClient
    {
        private readonly EntryClient _entryApi;
        private readonly IVaultKeyCache _vaultKeyCache;
        private readonly IAesGcmService _aesGcm;

        public ImportExportClient(EntryClient entryApi, IVaultKeyCache vaultKeyCache, IAesGcmService aesGcm)
        {
            _entryApi = entryApi;
            _vaultKeyCache = vaultKeyCache;
            _aesGcm = aesGcm;
        }

        // Source column name per format for each of our 5 fields. Url/Notes are null when the
        // format has no equivalent column — the row still imports, just without that field.
        private static readonly Dictionary<ImportFormat, (string Name, string Username, string Password, string? Url, string? Notes)> ColumnMap = new()
        {
            [ImportFormat.LastPass] = ("name", "username", "password", "url", "extra"),
            [ImportFormat.Bitwarden] = ("name", "login_username", "login_password", "login_uri", "notes"),
            [ImportFormat.OnePassword] = ("Title", "Username", "Password", "Website", "Notes"),
        };

        /// <summary>Imports every row of <paramref name="csv"/> into <paramref name="vaultId"/>.
        /// A row missing name/username/password, or a column the chosen format doesn't have, is
        /// recorded in <see cref="ImportResult.Errors"/> and skipped — it never aborts the rest
        /// of the file.</summary>
        public async Task<ImportResult> ImportAsync(Stream csv, ImportFormat format, int vaultId)
        {
            var vaultKey = _vaultKeyCache.Get(vaultId)
                ?? throw new InvalidOperationException("Your session is missing this vault's encryption key — log out and log back in.");
            var columns = ColumnMap[format];
            var result = new ImportResult();

            using var textReader = new StreamReader(csv);
            using var csvReader = new CsvReader(textReader, new CsvConfiguration(CultureInfo.InvariantCulture));

            if (!await csvReader.ReadAsync() || !csvReader.ReadHeader())
            {
                result.Errors.Add(new ImportRowError { Row = 1, Reason = "The file has no header row." });
                return result;
            }

            var row = 1; // the header itself is row 1
            while (await csvReader.ReadAsync())
            {
                row++;
                try
                {
                    var name = csvReader.GetField(columns.Name);
                    var username = csvReader.GetField(columns.Username);
                    var password = csvReader.GetField(columns.Password);
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    {
                        result.Errors.Add(new ImportRowError { Row = row, Reason = "Missing name, username or password." });
                        continue;
                    }

                    var url = columns.Url is null ? null : csvReader.GetField(columns.Url);
                    var notes = columns.Notes is null ? null : csvReader.GetField(columns.Notes);

                    await _entryApi.CreateAsync(new EntryCreateRequest
                    {
                        VaultId = vaultId,
                        Name = await _aesGcm.EncryptAsync(vaultKey, name),
                        Username = await _aesGcm.EncryptAsync(vaultKey, username),
                        Password = await _aesGcm.EncryptAsync(vaultKey, password),
                        Url = string.IsNullOrEmpty(url) ? null : await _aesGcm.EncryptAsync(vaultKey, url),
                        Notes = string.IsNullOrEmpty(notes) ? null : await _aesGcm.EncryptAsync(vaultKey, notes),
                    });
                    result.Imported++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportRowError { Row = row, Reason = ex.Message });
                }
            }

            return result;
        }
    }
}
