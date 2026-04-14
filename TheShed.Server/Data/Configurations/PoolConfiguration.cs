using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data.Configurations
{
    public class PoolConfiguration : IEntityTypeConfiguration<Pool>
    {
        public void Configure(EntityTypeBuilder<Pool> entity)
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.City).HasMaxLength(100);
            entity.Property(p => p.Country).HasMaxLength(100);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Pools_LengthMeters",
                "[LengthMeters] IN (25, 50)"));
        }
    }
}
