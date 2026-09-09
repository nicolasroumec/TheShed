using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Pages;

public partial class Register
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly RegisterRequest _model = new();
    private string? _error;
    private bool _busy;

    private async Task HandleSubmit()
    {
        _busy = true;
        _error = null;

        var result = await AuthService.RegisterAsync(_model);
        _busy = false;

        if (result.Success)
        {
            Navigation.NavigateTo("");
        }
        else
        {
            _error = result.Error;
        }
    }
}
