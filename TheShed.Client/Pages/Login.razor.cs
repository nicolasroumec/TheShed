using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Pages;

public partial class Login
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    private readonly LoginRequest _model = new();
    private string? _error;
    private bool _busy;
    private bool _needsCode;

    private async Task HandleSubmit()
    {
        _busy = true;
        _error = null;

        var result = await AuthService.LoginAsync(_model);
        _busy = false;

        if (result.Success)
        {
            Navigation.NavigateTo(string.IsNullOrWhiteSpace(ReturnUrl) ? "" : ReturnUrl);
        }
        else
        {
            // The first "code required" 401 isn't an error, just the next step.
            _error = result.TwoFactorRequired && !_needsCode ? null : result.Error;
            _needsCode = result.TwoFactorRequired;
            _model.TotpCode = null;
        }
    }
}
