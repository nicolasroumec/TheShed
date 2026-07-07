using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Tags
{
    /// <summary>Data to rename a tag.</summary>
    public class TagUpdateRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
