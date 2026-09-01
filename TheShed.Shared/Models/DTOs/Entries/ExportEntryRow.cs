namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>One CSV row for export (Sprint 17). All fields here are plaintext — the caller
    /// decrypts with the vault key before mapping into this, and CsvHelper writes it as-is.</summary>
    public class ExportEntryRow
    {
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Notes { get; set; }
    }
}
