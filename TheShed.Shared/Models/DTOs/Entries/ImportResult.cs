namespace TheShed.Shared.Models.DTOs.Entries
{
    /// <summary>Outcome of a CSV import run (Sprint 17): how many rows were created and, for the
    /// ones that weren't, why — one entry per skipped row, not just a count.</summary>
    public class ImportResult
    {
        public int Imported { get; set; }
        public List<ImportRowError> Errors { get; set; } = [];
    }

    /// <summary>One row that failed to import. Row is 1-based and counts the header, matching
    /// what a user sees when opening the CSV in a spreadsheet app.</summary>
    public class ImportRowError
    {
        public int Row { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
