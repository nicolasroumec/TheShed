using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data.Configurations
{
    public class StartAnalysisConfiguration : IEntityTypeConfiguration<StartAnalysis>
    {
        public void Configure(EntityTypeBuilder<StartAnalysis> entity)
        {
            entity.HasKey(sa => sa.Id);

            entity.HasOne(sa => sa.SwimPerformance)
                .WithOne(sp => sp.StartAnalysis)
                .HasForeignKey<StartAnalysis>(sa => sa.SwimPerformanceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
