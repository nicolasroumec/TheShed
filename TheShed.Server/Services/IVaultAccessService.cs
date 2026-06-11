using TheShed.Server.Enums;

namespace TheShed.Server.Services
{
    public interface IVaultAccessService
    {
        /// <summary>Resolves the access level <paramref name="userId"/> has over the
        /// vault. The owner and Editor members get Write, Viewer members get Read,
        /// anyone else (or a missing/deleted vault) gets None.</summary>
        Task<VaultAccess> GetAccessAsync(int vaultId, int userId, CancellationToken ct = default);
    }
}
