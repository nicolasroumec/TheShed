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
            _error = result.Error;
        }
    }
}
