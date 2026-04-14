using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TheShed.Shared.Models.Base;
using TheShed.Shared.Models.Entities;

namespace TheShed.Server.Data
{
    public class TheShedContext : DbContext
    {
        public TheShedContext(DbContextOptions<TheShedContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Country> Countries => Set<Country>();
        public DbSet<Club> Clubs => Set<Club>();
        public DbSet<Pool> Pools => Set<Pool>();
        public DbSet<Competition> Competitions => Set<Competition>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<SwimPerformance> SwimPerformances => Set<SwimPerformance>();
        public DbSet<StartAnalysis> StartAnalyses => Set<StartAnalysis>();
        public DbSet<TurnAnalysis> TurnAnalyses => Set<TurnAnalysis>();
        public DbSet<Split> Splits => Set<Split>();
        public DbSet<FinishAnalysis> FinishAnalyses => Set<FinishAnalysis>();
        public DbSet<AnalystComment> AnalystComments => Set<AnalystComment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aplica todas las clases *Configuration del assembly automáticamente
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Country: índice único en IsoCode (entidad simple, no necesita archivo propio)
            modelBuilder.Entity<Country>().HasIndex(c => c.IsoCode).IsUnique();

            // Soft delete global: filtra IsDeleted = false en todas las queries
            // Solo aplica a entidades que heredan de AuditableEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
                    var condition = Expression.Equal(property, Expression.Constant(false));
                    var lambda = Expression.Lambda(condition, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        break;
                    case EntityState.Deleted:
                        // Convierte borrado físico en soft delete
                        entry.Entity.IsDeleted = true;
                        entry.Entity.UpdatedAt = now;
                        entry.State = EntityState.Modified;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
