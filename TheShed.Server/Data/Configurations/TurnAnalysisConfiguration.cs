using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data.Configurations
{
    public class TurnAnalysisConfiguration : IEntityTypeConfiguration<TurnAnalysis>
    {
        public void Configure(EntityTypeBuilder<TurnAnalysis> entity)
        {
            entity.HasKey(ta => ta.Id);

            // Índice compuesto: una performance no puede tener dos virajes en la misma distancia
            entity.HasIndex(ta => new { ta.SwimPerformanceId, ta.TurnDistanceMeters }).IsUnique();

            entity.HasOne(ta => ta.SwimPerformance)
                .WithMany(sp => sp.TurnAnalyses)
                .HasForeignKey(ta => ta.SwimPerformanceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
