using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data.Configurations
{
    public class SwimPerformanceConfiguration : IEntityTypeConfiguration<SwimPerformance>
    {
        public void Configure(EntityTypeBuilder<SwimPerformance> entity)
        {
            entity.HasKey(sp => sp.Id);
            entity.Property(sp => sp.VideoUrl).IsRequired().HasMaxLength(500);
            entity.Property(sp => sp.AnalysisNotes).HasMaxLength(2000);

            entity.HasIndex(sp => sp.SwimmerId);
            entity.HasIndex(sp => sp.AnalystId);
            entity.HasIndex(sp => sp.Status);
            entity.HasIndex(sp => sp.UploadedAt);

            // Dos FK a la misma tabla Users → Restrict para no borrar en cascada
            entity.HasOne(sp => sp.Swimmer)
                .WithMany()
                .HasForeignKey(sp => sp.SwimmerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sp => sp.Analyst)
                .WithMany()
                .HasForeignKey(sp => sp.AnalystId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(sp => sp.Competition)
                .WithMany(c => c.Performances)
                .HasForeignKey(sp => sp.CompetitionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(sp => sp.Event)
                .WithMany(e => e.Performances)
                .HasForeignKey(sp => sp.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
