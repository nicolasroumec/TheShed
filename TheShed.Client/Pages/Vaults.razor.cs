using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Vaults;

namespace TheShed.Client.Pages;

public partial class Vaults
{
    [Inject] private VaultClient VaultApi { get; set; } = default!;

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
        _busy = true;
        _error = null;
        try
        {
            await VaultApi.CreateAsync(_newVault);
            _newVault.Name = string.Empty;
            _newVault.Description = null;
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
