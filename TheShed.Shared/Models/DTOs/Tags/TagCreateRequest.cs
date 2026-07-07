using System.ComponentModel.DataAnnotations;

namespace TheShed.Shared.Models.DTOs.Tags
{
    /// <summary>Data to create a tag. Tags are per-user; the owner is taken from the JWT.</summary>
    public class TagCreateRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
