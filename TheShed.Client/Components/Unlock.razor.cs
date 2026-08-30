using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;

namespace TheShed.Client.Components;

public partial class Unlock
{
    [Inject] private IStretchedKeyStore KeyStore { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;

    /// <summary>The routed page, rendered only once the session holds its encryption key.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private readonly UnlockModel _model = new();
    private string? _error;
    private bool _busy;

    private async Task HandleSubmit()
    {
        _busy = true;
        _error = null;

        var result = await AuthService.UnlockAsync(_model.Password);

        _busy = false;
        // Held no longer than the derivation itself, right or wrong.
        _model.Password = string.Empty;

        if (!result.Success)
        {
            _error = result.Error;
        }
    }

    /// <summary>Escape hatch for an account that cannot be unlocked at all (no keys on the
    /// server), and for anyone who would rather start a fresh session.</summary>
    private Task SignOutAsync() => AuthService.LogoutAsync();

    private sealed class UnlockModel
    {
        public string Password { get; set; } = string.Empty;
    }
}
