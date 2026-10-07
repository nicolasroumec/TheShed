using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Pages;

public partial class Account
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private ChangePasswordRequest _model = new();
    // Kept off the DTO: it's a UI-only check and would otherwise ride along in the shared contract.
    private string _confirm = string.Empty;
    private string? _error;
    private bool _busy;
    private bool _done;

    private async Task LogoutEverywhere()
    {
        _busy = true;
        await AuthService.LogoutEverywhereAsync();
        Navigation.NavigateTo("login");
    }

    private async Task HandleSubmit()
    {
        _done = false;
        if (_model.NewPassword != _confirm)
        {
            _error = "The new passwords don't match.";
            return;
        }

        _busy = true;
        _error = null;

        var result = await AuthService.ChangePasswordAsync(_model);
        _busy = false;

        if (result.Success)
        {
            // Don't leave the passwords sitting in the form.
            _model = new();
            _confirm = string.Empty;
            _done = true;
        }
        else
        {
            _error = result.Error;
        }
    }
}
