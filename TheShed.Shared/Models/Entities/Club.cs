using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TheShed.Shared.Models.Entities
{
    public class Club
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<User> Members { get; set; } = new List<User>();
    }
}
