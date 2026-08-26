using TheShed.Shared.Models.DTOs.Vaults;

namespace TheShed.Client.Services
{
    /// <summary>
    /// Resolves a vault's AES key for the current session: returns it from
    /// <see cref="IVaultKeyCache"/> if already unwrapped, otherwise unwraps it — owner: AES under
    /// the stretched master key (Sprint 26); member: RSA-OAEP under their own keypair (Sprint 27)
    /// — and caches the result. Shared by <c>VaultDetail</c> (opening one vault) and
    /// <c>Health</c> (auditing every accessible vault) so the owner/member branch lives in one
    /// place instead of two.
    /// </summary>
    public interface IVaultKeyResolver
    {
        /// <summary>Null if nothing here can unwrap the key this session — no stretched master
        /// key or own keypair cached (e.g. after a page reload), or the vault has no wrap yet.</summary>
        Task<byte[]?> ResolveAsync(VaultResponse vault);
    }
}
