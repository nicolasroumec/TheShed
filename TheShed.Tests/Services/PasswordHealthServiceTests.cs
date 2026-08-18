using Microsoft.EntityFrameworkCore;
using TheShed.Server.Data;
using TheShed.Server.Services;
using TheShed.Shared.Helpers;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Models.Enums;
using TheShed.Shared.Security;
using Xunit;

namespace TheShed.Tests.Services
{
    public class PasswordHealthServiceTests
    {
        // A fixed 32-byte key keeps encryption deterministic across a test's operations.
        private static readonly byte[] TestKey = new byte[32];

        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static (PasswordHealthService Service, AesEncryptionService Encryption) CreateService(TheShedContext db)
        {
            var encryption = new AesEncryptionService(TestKey);
            return (new PasswordHealthService(db, encryption), encryption);
        }

        private static async Task<(int userId, int vaultId)> SeedVaultAsync(TheShedContext db)
        {
            var owner = new User { Username = "owner", Email = "owner@test.com", PasswordHash = "h" };
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var vault = new Vault { OwnerId = owner.Id, Name = "Personal" };
            db.Vaults.Add(vault);
            await db.SaveChangesAsync();

            return (owner.Id, vault.Id);
        }

        [Fact]
        public async Task GetReportAsync_RatesStrengthPerEntry()
        {
            using var db = CreateContext();
            var (userId, vaultId) = await SeedVaultAsync(db);
            var (service, encryption) = CreateService(db);

            db.PasswordEntries.Add(new PasswordEntry { VaultId = vaultId, Name = "Weak one", Username = "u", PasswordEncrypted = encryption.Encrypt("abc") });
            db.PasswordEntries.Add(new PasswordEntry { VaultId = vaultId, Name = "Strong one", Username = "u", PasswordEncrypted = encryption.Encrypt("Tr7$kQmz!pLxN9wq") });
            await db.SaveChangesAsync();

            var report = await service.GetReportAsync(userId);

            Assert.Equal(PasswordStrength.Weak, report.Single(i => i.EntryName == "Weak one").Strength);
            Assert.Equal(PasswordStrength.Strong, report.Single(i => i.EntryName == "Strong one").Strength);
        }

        [Fact]
        public async Task GetReportAsync_SharedPassword_FlagsBothAsReused()
        {
            using var db = CreateContext();
            var (userId, vaultId) = await SeedVaultAsync(db);
            var (service, encryption) = CreateService(db);

            db.PasswordEntries.Add(new PasswordEntry { VaultId = vaultId, Name = "Site A", Username = "u", PasswordEncrypted = encryption.Encrypt("shared-pass") });
            db.PasswordEntries.Add(new PasswordEntry { VaultId = vaultId, Name = "Site B", Username = "u", PasswordEncrypted = encryption.Encrypt("shared-pass") });
            db.PasswordEntries.Add(new PasswordEntry { VaultId = vaultId, Name = "Site C", Username = "u", PasswordEncrypted = encryption.Encrypt("unique-pass") });
            await db.SaveChangesAsync();

            var report = await service.GetReportAsync(userId);

            Assert.True(report.Single(i => i.EntryName == "Site A").IsReused);
            Assert.True(report.Single(i => i.EntryName == "Site B").IsReused);
            Assert.False(report.Single(i => i.EntryName == "Site C").IsReused);
        }

        [Fact]
        public async Task GetReportAsync_IncludesSharedVaultEntries()
        {
            using var db = CreateContext();
            var (ownerId, vaultId) = await SeedVaultAsync(db);
            var (service, encryption) = CreateService(db);

            var member = new User { Username = "member", Email = "member@test.com", PasswordHash = "h" };
            db.Users.Add(member);
            await db.SaveChangesAsync();
            db.VaultMembers.Add(new VaultMember { VaultId = vaultId, UserId = member.Id, Role = VaultRole.Viewer });
            db.PasswordEntries.Add(new PasswordEntry { VaultId = vaultId, Name = "Shared vault entry", Username = "u", PasswordEncrypted = encryption.Encrypt("whatever123!") });
            await db.SaveChangesAsync();

            var report = await service.GetReportAsync(member.Id);

            Assert.Contains(report, i => i.EntryName == "Shared vault entry");
        }

        [Fact]
        public async Task GetReportAsync_NoAccessibleVaults_ReturnsEmpty()
        {
            using var db = CreateContext();
            var stranger = new User { Username = "stranger", Email = "stranger@test.com", PasswordHash = "h" };
            db.Users.Add(stranger);
            await db.SaveChangesAsync();
            var (service, _) = CreateService(db);

            var report = await service.GetReportAsync(stranger.Id);

            Assert.Empty(report);
        }
    }
}
