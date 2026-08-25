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
    [Inject] private IStretchedKeyStore KeyStore { get; set; } = default!;
    [Inject] private IVaultKeyCache VaultKeyCache { get; set; } = default!;

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

        // No wrap yet for a shared member (Sprint 27), or no stretched key in this session (e.g.
        // pre-Sprint-25 account not migrated yet) — nothing to unwrap. Entries still fall back to
        // server-side decryption until Sprint 26's client-side move is complete for all of them.
        var stretchedMasterKey = KeyStore.Get();
        if (_vault?.WrappedKey is not null && stretchedMasterKey is not null)
        {
            var vaultKey = await VaultKey.UnwrapKeyAsync(stretchedMasterKey, _vault.WrappedKey);
            VaultKeyCache.Set(Id, vaultKey);
        }
    }
}
