using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TheShed.Server.Data;
using TheShed.Server.Services;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Models.DTOs.Vaults;
using TheShed.Shared.Models.Entities;
using TheShed.Shared.Security;
using Xunit;

namespace TheShed.Tests.Security
{
    /// <summary>
    /// End-to-end proof of Sprint 26's zero-knowledge property. AesEncryptionService/
    /// KeyDerivationService run outside browser-wasm here (a plain xunit test), but produce
    /// byte-for-byte the same wire format as the real client path (WebCryptoVaultKeyService /
    /// WebCryptoAesGcmService, both documented as compatible) — so using them to play the
    /// client's part is faithful to what actually happens in the browser, just without needing
    /// one. What matters is asserted against the real server-side services and a real
    /// (in-memory) database: what lands there cannot be turned back into plaintext without the
    /// master password, and the wrong password fails loudly instead of returning garbage.
    /// </summary>
    public class ZeroKnowledgeE2ETests
    {
        private static TheShedContext CreateContext() =>
            new(new DbContextOptionsBuilder<TheShedContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        [Fact]
        public async Task FullClientFlow_ServerNeverSeesPlaintext_AndWrongPasswordCannotDecrypt()
        {
            using var db = CreateContext();
            var kdf = new KeyDerivationService();

            // --- Register: derive the stretched master key from the master password (Increment 1).
            // Never sent to the server — only KeySalt is. ---
            const string masterPassword = "correct horse battery staple";
            var keySalt = kdf.GenerateSalt();
            var stretchedMasterKey = await kdf.DeriveKeyAsync(masterPassword, keySalt);

            var user = new User
            {
                Username = "ana",
                Email = "ana@test.com",
                PasswordHash = "argon2-hash", // unrelated to the stretched key; Argon2 auth vs. PBKDF2 encryption are independent (Sprint 25)
                KeySalt = Convert.ToBase64String(keySalt)
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // --- Create vault: client generates a random vault key and wraps it with the
            // stretched master key (Increment 2). EncryptBytes, not Encrypt(string): the real
            // interop call encrypts the vault key's raw bytes, not a base64 string of them. ---
            var vaultKey = RandomNumberGenerator.GetBytes(32);
            var masterKeyCrypto = new AesEncryptionService(stretchedMasterKey);
            var wrappedVaultKey = Convert.ToBase64String(masterKeyCrypto.EncryptBytes(vaultKey));

            var vaultService = new VaultService(db, new VaultAccessService(db));
            var vault = await vaultService.CreateAsync(user.Id, new VaultCreateRequest
            {
                Name = "Personal",
                VaultKeyWrap = wrappedVaultKey
            });

            // --- Create entry: client encrypts every sensitive field with the vault key
            // (Increments 4 and 6). ---
            const string realPassword = "hunter2-super-secret";
            var vaultKeyCrypto = new AesEncryptionService(vaultKey);
            var entryService = new PasswordEntryService(db, new VaultAccessService(db));
            var created = await entryService.CreateAsync(user.Id, new EntryCreateRequest
            {
                VaultId = vault.Id,
                Name = vaultKeyCrypto.Encrypt("Gmail"),
                Username = vaultKeyCrypto.Encrypt("ana@gmail.com"),
                Password = vaultKeyCrypto.Encrypt(realPassword),
                Url = vaultKeyCrypto.Encrypt("https://gmail.com")
            });
            Assert.True(created.Success);

            // --- What actually landed in the database. ---
            var storedEntry = await db.PasswordEntries.SingleAsync();
            var storedWrap = await db.VaultKeyWraps.SingleAsync();

            // 1) Nothing sensitive is stored in the clear.
            Assert.NotEqual(realPassword, storedEntry.PasswordEncrypted);
            Assert.NotEqual("Gmail", storedEntry.Name);
            Assert.NotEqual("ana@gmail.com", storedEntry.Username);
            Assert.NotEqual(Convert.ToBase64String(vaultKey), storedWrap.WrappedKey);

            // 2) The correct master password reconstructs everything, starting from nothing but
            // the salt (public) and what's in the database (also, by assumption, in the
            // attacker's or the operator's hands) — proving the round trip is real, not just
            // "the plaintext never touched the wire" by accident.
            var rederivedStretchedKey = await kdf.DeriveKeyAsync(masterPassword, Convert.FromBase64String(user.KeySalt!));
            var recoveredVaultKey = new AesEncryptionService(rederivedStretchedKey)
                .DecryptBytes(Convert.FromBase64String(storedWrap.WrappedKey));
            Assert.Equal(vaultKey, recoveredVaultKey);

            var recoveredCrypto = new AesEncryptionService(recoveredVaultKey);
            Assert.Equal(realPassword, recoveredCrypto.Decrypt(storedEntry.PasswordEncrypted));
            Assert.Equal("Gmail", recoveredCrypto.Decrypt(storedEntry.Name));
            Assert.Equal("ana@gmail.com", recoveredCrypto.Decrypt(storedEntry.Username));

            // 3) The WRONG master password cannot decrypt anything — AES-GCM's authentication
            // tag makes a wrong key fail loudly (throws) instead of silently returning wrong
            // bytes. And there is no server-side master key that could stand in for it either:
            // VaultService and PasswordEntryService take no IEncryptionService dependency any
            // more (see their constructors) — there's nothing left on the server that could even
            // attempt this.
            var wrongStretchedKey = await kdf.DeriveKeyAsync("wrong password", Convert.FromBase64String(user.KeySalt!));
            var wrongUnwrapper = new AesEncryptionService(wrongStretchedKey);
            Assert.Throws<AuthenticationTagMismatchException>(() =>
                wrongUnwrapper.DecryptBytes(Convert.FromBase64String(storedWrap.WrappedKey)));
        }
    }
}
