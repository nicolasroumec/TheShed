using TheShed.Shared.Models.Enums;

namespace TheShed.Shared.Models.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public float? Height { get; set; }
        public float? Weight { get; set; }
        public float? Wingspan { get; set; }
        public int? ClubId { get; set; }
        public Club? Club { get; set; }
        public int? CountryId { get; set; }
        public Country? Country { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public UserRole Role { get; set; }

    }
}
