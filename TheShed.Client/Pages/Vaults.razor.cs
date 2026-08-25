using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Vaults;
using TheShed.Shared.Security;

namespace TheShed.Client.Pages;

public partial class Vaults
{
    [Inject] private VaultClient VaultApi { get; set; } = default!;
    [Inject] private IVaultKeyService VaultKey { get; set; } = default!;
    [Inject] private IStretchedKeyStore KeyStore { get; set; } = default!;

    private IReadOnlyList<VaultListItem>? _vaults;
    private readonly VaultCreateRequest _newVault = new();
    private bool _showCreate;
    private bool _busy;
    private string? _error;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _vaults = await VaultApi.ListAsync();
    }

    private static string RoleLabel(VaultListItem v) => v.IsOwner ? "Owner" : v.CanWrite ? "Editor" : "Viewer";

    private void ToggleCreate()
    {
        _showCreate = !_showCreate;
        _error = null;
    }

    private async Task CreateAsync()
    {
        var stretchedMasterKey = KeyStore.Get();
        if (stretchedMasterKey is null)
        {
            _error = "Your session is missing its encryption key — log out and log back in.";
            return;
        }

        _busy = true;
        _error = null;
        try
        {
            _newVault.VaultKeyWrap = await VaultKey.GenerateWrappedKeyAsync(stretchedMasterKey);
            await VaultApi.CreateAsync(_newVault);
            _newVault.Name = string.Empty;
            _newVault.Description = null;
            _newVault.VaultKeyWrap = string.Empty;
            _showCreate = false;
            await LoadAsync();
        }
        catch (Exception)
        {
            _error = "Could not create the vault. Please try again.";
        }
        finally
        {
            _busy = false;
        }
    }
}
