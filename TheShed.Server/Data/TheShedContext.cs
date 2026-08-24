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
        public DbSet<Attachment> Attachments => Set<Attachment>();
        public DbSet<EntryHistory> EntryHistory => Set<EntryHistory>();
        public DbSet<PasswordEntry> PasswordEntries => Set<PasswordEntry>();
        public DbSet<PasswordEntryTag> PasswordEntryTags => Set<PasswordEntryTag>();
        public DbSet<SecureNote> SecureNotes => Set<SecureNote>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Vault> Vaults => Set<Vault>();
        public DbSet<VaultMember> VaultMembers => Set<VaultMember>();
        public DbSet<VaultKeyWrap> VaultKeyWraps => Set<VaultKeyWrap>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // SQL Server rechaza múltiples caminos de cascada hacia una misma tabla.
            // En cada caso se conserva la cascada por el "dueño" (Vault / PasswordEntry)
            // y se restringe la segunda ruta (vía User) a NO ACTION.
            modelBuilder.Entity<VaultMember>()
                .HasOne(vm => vm.User)
                .WithMany(u => u.VaultMemberships)
                .HasForeignKey(vm => vm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VaultKeyWrap>()
                .HasOne(vkw => vkw.User)
                .WithMany()
                .HasForeignKey(vkw => vkw.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EntryHistory>()
                .HasOne(eh => eh.ChangedBy)
                .WithMany()
                .HasForeignKey(eh => eh.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PasswordEntryTag>()
                .HasOne(pet => pet.Tag)
                .WithMany(t => t.PasswordEntries)
                .HasForeignKey(pet => pet.TagId)
                .OnDelete(DeleteBehavior.Restrict);

            // Soft delete global: filtra IsDeleted = false en todas las queries
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
                        if (entry.Entity.IsDeleted)
                        {
                            // Already soft-deleted: Remove() here means a real purge from the
                            // trash. Leave the state as Deleted so EF issues a hard DELETE.
                            break;
                        }
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = now;
                        entry.Entity.UpdatedAt = now;
                        entry.State = EntityState.Modified;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
