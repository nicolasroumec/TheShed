using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data.Configurations
{
    public class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
    {
        public void Configure(EntityTypeBuilder<Competition> entity)
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Location).HasMaxLength(200);
            entity.Property(c => c.Organizer).HasMaxLength(200);

            entity.HasIndex(c => c.Date);

            entity.HasOne(c => c.Pool)
                .WithMany(p => p.Competitions)
                .HasForeignKey(c => c.PoolId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
