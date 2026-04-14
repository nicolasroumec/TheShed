using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> entity)
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Stroke, e.DistanceMeters, e.Gender }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Events_DistanceMeters",
                "[DistanceMeters] IN (50, 100, 200, 400, 800, 1500)"));
        }
    }
}
