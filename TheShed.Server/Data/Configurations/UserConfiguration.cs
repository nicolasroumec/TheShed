using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.Phone).HasMaxLength(50);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);

            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Country)
                .WithMany()
                .HasForeignKey(u => u.CountryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(u => u.Club)
                .WithMany(c => c.Members)
                .HasForeignKey(u => u.ClubId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
