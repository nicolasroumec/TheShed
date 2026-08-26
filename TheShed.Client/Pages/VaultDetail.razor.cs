using Microsoft.AspNetCore.Components;
using TheShed.Client.Components.Vault;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Vaults;

namespace TheShed.Client.Pages;

public partial class VaultDetail
{
    [Parameter] public int Id { get; set; }

    [Inject] private VaultClient VaultApi { get; set; } = default!;
    [Inject] private IVaultKeyResolver KeyResolver { get; set; } = default!;

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

        if (_vault is not null)
        {
            await KeyResolver.ResolveAsync(_vault);
        }
    }
}
