using TheShed.Shared.Models.DTOs.Vaults;
using TheShed.Shared.Security;

namespace TheShed.Client.Services
{
    /// <inheritdoc cref="IVaultKeyResolver"/>
    public class VaultKeyResolver : IVaultKeyResolver
    {
        private readonly IVaultKeyService _vaultKey;
        private readonly IUserKeypairService _userKeypair;
        private readonly IStretchedKeyStore _keyStore;
        private readonly IVaultKeyCache _vaultKeyCache;
        private readonly IOwnKeypairCache _ownKeypair;

        public VaultKeyResolver(IVaultKeyService vaultKey, IUserKeypairService userKeypair,
            IStretchedKeyStore keyStore, IVaultKeyCache vaultKeyCache, IOwnKeypairCache ownKeypair)
        {
            _vaultKey = vaultKey;
            _userKeypair = userKeypair;
            _keyStore = keyStore;
            _vaultKeyCache = vaultKeyCache;
            _ownKeypair = ownKeypair;
        }

        public async Task<byte[]?> ResolveAsync(VaultResponse vault)
        {
            var cached = _vaultKeyCache.Get(vault.Id);
            if (cached is not null)
            {
                return cached;
            }

            var stretchedMasterKey = _keyStore.Get();
            if (vault.WrappedKey is null || stretchedMasterKey is null)
            {
                return null;
            }

            byte[] vaultKey;
            if (vault.IsOwner)
            {
                vaultKey = await _vaultKey.UnwrapKeyAsync(stretchedMasterKey, vault.WrappedKey);
            }
            else
            {
                var (_, encryptedPrivateKey) = _ownKeypair.Get();
                if (encryptedPrivateKey is null)
                {
                    return null;
                }
                vaultKey = await _userKeypair.UnwrapKeyAsMemberAsync(stretchedMasterKey, encryptedPrivateKey, vault.WrappedKey);
            }

            _vaultKeyCache.Set(vault.Id, vaultKey);
            return vaultKey;
        }
    }
}
