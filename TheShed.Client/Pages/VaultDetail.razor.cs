using Microsoft.AspNetCore.Components;
using TheShed.Client.Components.Vault;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Vaults;
using TheShed.Shared.Security;

namespace TheShed.Client.Pages;

public partial class VaultDetail
{
    [Parameter] public int Id { get; set; }

    [Inject] private VaultClient VaultApi { get; set; } = default!;
    [Inject] private IVaultKeyService VaultKey { get; set; } = default!;
    [Inject] private IUserKeypairService UserKeypair { get; set; } = default!;
    [Inject] private IStretchedKeyStore KeyStore { get; set; } = default!;
    [Inject] private IVaultKeyCache VaultKeyCache { get; set; } = default!;
    [Inject] private IOwnKeypairCache OwnKeypair { get; set; } = default!;

    private VaultResponse? _vault;
    private bool _notFound;
    private EntriesPanel? _entriesPanel;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _vault = await VaultApi.GetAsync(Id);
        }
        catch (HttpRequestException)
        {
            // 404/403 from the API: vault missing or no access.
            _notFound = true;
            return;
        }

        // No stretched key in this session (e.g. a page reload — nothing here survives that, same
        // as a pre-Sprint-25 account not migrated yet) — nothing to unwrap.
        var stretchedMasterKey = KeyStore.Get();
        if (_vault?.WrappedKey is null || stretchedMasterKey is null)
        {
            return;
        }

        // Owner: the wrap is AES, under their own stretched master key (Sprint 26). Member: the
        // wrap is RSA-OAEP, under their public key (Sprint 27) — unwrap with their own private
        // key instead, itself AES-wrapped under the same stretched master key.
        byte[] vaultKey;
        if (_vault.IsOwner)
        {
            vaultKey = await VaultKey.UnwrapKeyAsync(stretchedMasterKey, _vault.WrappedKey);
        }
        else
        {
            var (_, encryptedPrivateKey) = OwnKeypair.Get();
            if (encryptedPrivateKey is null)
            {
                return; // no keypair cached this session (page reload, or a pre-Sprint-25 account)
            }
            vaultKey = await UserKeypair.UnwrapKeyAsMemberAsync(stretchedMasterKey, encryptedPrivateKey, _vault.WrappedKey);
        }
        VaultKeyCache.Set(Id, vaultKey);
    }
}
