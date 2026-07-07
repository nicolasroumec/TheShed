namespace TheShed.Shared.Models.DTOs.Tags
{
    /// <summary>A tag, as returned in listings and embedded in entries.</summary>
    public class TagResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
