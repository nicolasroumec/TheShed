using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data.Configurations
{
    public class FinishAnalysisConfiguration : IEntityTypeConfiguration<FinishAnalysis>
    {
        public void Configure(EntityTypeBuilder<FinishAnalysis> entity)
        {
            entity.HasKey(fa => fa.Id);

            entity.HasOne(fa => fa.SwimPerformance)
                .WithOne(sp => sp.FinishAnalysis)
                .HasForeignKey<FinishAnalysis>(fa => fa.SwimPerformanceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
