using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data.Configurations
{
    public class SplitEntityConfiguration : IEntityTypeConfiguration<Split>
    {
        public void Configure(EntityTypeBuilder<Split> entity)
        {
            entity.HasKey(s => s.Id);

            // Índice compuesto: una performance no puede tener dos splits en la misma distancia
            entity.HasIndex(s => new { s.SwimPerformanceId, s.DistanceMeters }).IsUnique();

            entity.HasOne(s => s.SwimPerformance)
                .WithMany(sp => sp.Splits)
                .HasForeignKey(s => s.SwimPerformanceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
