using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheShed.Server.Data;
using TheShed.Server.Enums;
using TheShed.Server.Services;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Models.Enums;
using Xunit;

namespace TheShed.Tests.Services
{
    public class TrashServiceTests
    {
        private const int RetentionDays = 30;
        private const int StrangerId = 9999; // a user with no access to the seeded vault

        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static TrashService CreateService(TheShedContext db, IAttachmentStorage? storage = null) =>
            new(db, new VaultAccessService(db), storage ?? new InMemoryAttachmentStorage(),
                Options.Create(new TrashSettings { RetentionDays = RetentionDays }));

        // A soft-deleted entry with one attachment whose blob sits in storage under blobKey.
        private static async Task<int> SeedDeletedEntryWithAttachmentAsync(
            TheShedContext db, InMemoryAttachmentStorage storage, int vaultId, string blobKey)
        {
            var entry = new PasswordEntry { VaultId = vaultId, Name = "Bank", Username = "a", PasswordEncrypted = "x" };
            entry.Attachments.Add(new Attachment { FileName = "scan.pdf", StoragePath = blobKey, FileSizeBytes = 3 });
            db.PasswordEntries.Add(entry);
            await db.SaveChangesAsync();
            await storage.SaveAsync(blobKey, [1, 2, 3]);

            db.PasswordEntries.Remove(entry);
            await db.SaveChangesAsync();

            return entry.Id;
        }

        private static async Task<(int ownerId, int vaultId)> SeedVaultAsync(TheShedContext db)
        {
            var owner = new User { Username = "owner", Email = "owner@test.com", PasswordHash = "h" };
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var vault = new Vault { OwnerId = owner.Id, Name = "Personal" };
            db.Vaults.Add(vault);
            await db.SaveChangesAsync();

            return (owner.Id, vault.Id);
        }

        private static async Task<int> AddMemberAsync(TheShedContext db, int vaultId, VaultRole role)
        {
            var user = new User { Username = "member", Email = $"m{Guid.NewGuid():N}@test.com", PasswordHash = "h" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.VaultMembers.Add(new VaultMember { VaultId = vaultId, UserId = user.Id, Role = role });
            await db.SaveChangesAsync();

            return user.Id;
        }

        private static async Task<int> SeedDeletedEntryAsync(TheShedContext db, int vaultId)
        {
            var entry = new PasswordEntry { VaultId = vaultId, Name = "Gmail", Username = "a", PasswordEncrypted = "x" };
            db.PasswordEntries.Add(entry);
            await db.SaveChangesAsync();

            db.PasswordEntries.Remove(entry);
            await db.SaveChangesAsync();

            return entry.Id;
        }

        private static async Task<int> SeedDeletedNoteAsync(TheShedContext db, int vaultId)
        {
            var note = new SecureNote { VaultId = vaultId, Title = "Codes", ContentEncrypted = "x" };
            db.SecureNotes.Add(note);
            await db.SaveChangesAsync();

            db.SecureNotes.Remove(note);
            await db.SaveChangesAsync();

            return note.Id;
        }

        // --- List ---

        [Fact]
        public async Task ListAsync_ReturnsDeletedVaultsEntriesAndNotes_ForOwner()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var entryId = await SeedDeletedEntryAsync(db, vaultId);
            var noteId = await SeedDeletedNoteAsync(db, vaultId);
            var service = CreateService(db);

            var items = await service.ListAsync(ownerId);

            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.Type == TrashItemType.Entry && i.Id == entryId);
            Assert.Contains(items, i => i.Type == TrashItemType.Note && i.Id == noteId);
            Assert.All(items, i => Assert.Equal(i.DeletedAt.AddDays(RetentionDays), i.ExpiresAt));
        }

        [Fact]
        public async Task ListAsync_IncludesDeletedVault_ButNotItsChildEntries()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            await SeedDeletedEntryAsync(db, vaultId);

            var vault = await db.Vaults.FirstAsync(v => v.Id == vaultId);
            db.Vaults.Remove(vault);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var items = await service.ListAsync(ownerId);

            var item = Assert.Single(items);
            Assert.Equal(TrashItemType.Vault, item.Type);
        }

        [Fact]
        public async Task ListAsync_ExcludesOtherUsersTrash()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            await SeedDeletedEntryAsync(db, vaultId);
            var service = CreateService(db);

            var items = await service.ListAsync(StrangerId);

            Assert.Empty(items);
        }

        // --- Restore ---

        [Fact]
        public async Task RestoreAsync_Entry_UnsetsIsDeletedAndDeletedAt()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var entryId = await SeedDeletedEntryAsync(db, vaultId);
            var service = CreateService(db);

            var result = await service.RestoreAsync(ownerId, TrashItemType.Entry, entryId);

            Assert.True(result.Success);
            var entry = await db.PasswordEntries.IgnoreQueryFilters().FirstAsync(e => e.Id == entryId);
            Assert.False(entry.IsDeleted);
            Assert.Null(entry.DeletedAt);
        }

        [Fact]
        public async Task RestoreAsync_Vault_UnsetsIsDeletedAndDeletedAt()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var vault = await db.Vaults.FirstAsync(v => v.Id == vaultId);
            db.Vaults.Remove(vault);
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.RestoreAsync(ownerId, TrashItemType.Vault, vaultId);

            Assert.True(result.Success);
            var restored = await db.Vaults.IgnoreQueryFilters().FirstAsync(v => v.Id == vaultId);
            Assert.False(restored.IsDeleted);
            Assert.Null(restored.DeletedAt);
        }

        [Fact]
        public async Task RestoreAsync_NonMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var entryId = await SeedDeletedEntryAsync(db, vaultId);
            var service = CreateService(db);

            var result = await service.RestoreAsync(StrangerId, TrashItemType.Entry, entryId);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        [Fact]
        public async Task RestoreAsync_ViewerMember_ReturnsForbidden()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var entryId = await SeedDeletedEntryAsync(db, vaultId);
            var viewerId = await AddMemberAsync(db, vaultId, VaultRole.Viewer);
            var service = CreateService(db);

            var result = await service.RestoreAsync(viewerId, TrashItemType.Entry, entryId);

            Assert.False(result.Success);
            Assert.Equal(EntryError.Forbidden, result.Error);
        }

        [Fact]
        public async Task RestoreAsync_Vault_NonOwnerMember_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var editorId = await AddMemberAsync(db, vaultId, VaultRole.Editor);
            var vault = await db.Vaults.FirstAsync(v => v.Id == vaultId);
            db.Vaults.Remove(vault);
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.RestoreAsync(editorId, TrashItemType.Vault, vaultId);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        // --- Purge (manual) ---

        [Fact]
        public async Task PurgeAsync_Entry_HardDeletesRow()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var entryId = await SeedDeletedEntryAsync(db, vaultId);
            var service = CreateService(db);

            var result = await service.PurgeAsync(ownerId, TrashItemType.Entry, entryId);

            Assert.True(result.Success);
            Assert.Null(await db.PasswordEntries.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entryId));
        }

        [Fact]
        public async Task PurgeAsync_Vault_HardDeletesRow()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var vault = await db.Vaults.FirstAsync(v => v.Id == vaultId);
            db.Vaults.Remove(vault);
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.PurgeAsync(ownerId, TrashItemType.Vault, vaultId);

            Assert.True(result.Success);
            Assert.Null(await db.Vaults.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.Id == vaultId));
        }

        [Fact]
        public async Task PurgeAsync_Entry_DeletesItsAttachmentBlobsOnly()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var storage = new InMemoryAttachmentStorage();
            var entryId = await SeedDeletedEntryWithAttachmentAsync(db, storage, vaultId, "purged");
            await SeedDeletedEntryWithAttachmentAsync(db, storage, vaultId, "kept");
            var service = CreateService(db, storage);

            var result = await service.PurgeAsync(ownerId, TrashItemType.Entry, entryId);

            Assert.True(result.Success);
            Assert.False(storage.Contains("purged"));
            Assert.True(storage.Contains("kept"));
        }

        [Fact]
        public async Task PurgeAsync_Vault_DeletesAttachmentBlobsOfItsEntries()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var storage = new InMemoryAttachmentStorage();
            await SeedDeletedEntryWithAttachmentAsync(db, storage, vaultId, "in-vault");
            // Like VaultService's fresh per-request context: with the entry still tracked, EF's
            // client-side cascade would hard-delete it (already IsDeleted) on the vault's soft delete.
            db.ChangeTracker.Clear();
            var vault = await db.Vaults.FirstAsync(v => v.Id == vaultId);
            db.Vaults.Remove(vault);
            await db.SaveChangesAsync();
            var service = CreateService(db, storage);

            var result = await service.PurgeAsync(ownerId, TrashItemType.Vault, vaultId);

            Assert.True(result.Success);
            Assert.False(storage.Contains("in-vault"));
        }

        [Fact]
        public async Task PurgeAsync_NotYetDeleted_ReturnsNotFound()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var entry = new PasswordEntry { VaultId = vaultId, Name = "Gmail", Username = "a", PasswordEncrypted = "x" };
            db.PasswordEntries.Add(entry);
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.PurgeAsync(ownerId, TrashItemType.Entry, entry.Id);

            Assert.False(result.Success);
            Assert.Equal(EntryError.NotFound, result.Error);
        }

        // --- Automatic purge (expiration) ---

        [Fact]
        public async Task PurgeExpiredAsync_RemovesOnlyItemsPastRetention()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var expiredEntryId = await SeedDeletedEntryAsync(db, vaultId);
            var freshEntryId = await SeedDeletedEntryAsync(db, vaultId);

            var expired = await db.PasswordEntries.IgnoreQueryFilters().FirstAsync(e => e.Id == expiredEntryId);
            expired.DeletedAt = DateTime.UtcNow.AddDays(-RetentionDays - 1);
            var fresh = await db.PasswordEntries.IgnoreQueryFilters().FirstAsync(e => e.Id == freshEntryId);
            fresh.DeletedAt = DateTime.UtcNow.AddDays(-RetentionDays + 1);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var purged = await service.PurgeExpiredAsync(DateTime.UtcNow);

            Assert.Equal(1, purged);
            Assert.Null(await db.PasswordEntries.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == expiredEntryId));
            Assert.NotNull(await db.PasswordEntries.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == freshEntryId));
        }

        [Fact]
        public async Task PurgeExpiredAsync_DeletesAttachmentBlobsOfExpiredEntriesOnly()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            var storage = new InMemoryAttachmentStorage();
            var expiredId = await SeedDeletedEntryWithAttachmentAsync(db, storage, vaultId, "expired");
            await SeedDeletedEntryWithAttachmentAsync(db, storage, vaultId, "fresh");
            var expired = await db.PasswordEntries.IgnoreQueryFilters().FirstAsync(e => e.Id == expiredId);
            expired.DeletedAt = DateTime.UtcNow.AddDays(-RetentionDays - 1);
            await db.SaveChangesAsync();
            var service = CreateService(db, storage);

            await service.PurgeExpiredAsync(DateTime.UtcNow);

            Assert.False(storage.Contains("expired"));
            Assert.True(storage.Contains("fresh"));
        }

        [Fact]
        public async Task PurgeExpiredAsync_IgnoresItemsThatAreNotDeleted()
        {
            using var db = CreateContext();
            var (_, vaultId) = await SeedVaultAsync(db);
            db.PasswordEntries.Add(new PasswordEntry { VaultId = vaultId, Name = "Gmail", Username = "a", PasswordEncrypted = "x" });
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var purged = await service.PurgeExpiredAsync(DateTime.UtcNow.AddYears(1));

            Assert.Equal(0, purged);
        }

        [Fact]
        public async Task PurgeExpiredAsync_SweepsVaultsEntriesNotesAndTags()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            await SeedDeletedEntryAsync(db, vaultId);
            await SeedDeletedNoteAsync(db, vaultId);

            var tag = new Tag { UserId = ownerId, Name = "work" };
            db.Tags.Add(tag);
            await db.SaveChangesAsync();
            db.Tags.Remove(tag);
            await db.SaveChangesAsync();

            var vault = await db.Vaults.FirstAsync(v => v.Id == vaultId);
            db.Vaults.Remove(vault);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var purged = await service.PurgeExpiredAsync(DateTime.UtcNow.AddDays(RetentionDays + 1));

            // Purging the vault cascades its already-soft-deleted entry/note away too, so they
            // don't add to the count from their own sweep — assert final state, not the tally.
            Assert.True(purged > 0);
            Assert.Null(await db.Vaults.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.Id == vaultId));
            Assert.Empty(await db.PasswordEntries.IgnoreQueryFilters().Where(e => e.VaultId == vaultId).ToListAsync());
            Assert.Empty(await db.SecureNotes.IgnoreQueryFilters().Where(n => n.VaultId == vaultId).ToListAsync());
            Assert.Empty(await db.Tags.IgnoreQueryFilters().Where(t => t.UserId == ownerId).ToListAsync());
        }
    }
}
