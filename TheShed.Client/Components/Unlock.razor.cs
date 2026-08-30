using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TheShed.Client.Services;

namespace TheShed.Client.Components;

public partial class Unlock : IAsyncDisposable
{
    // ponytail: one constant for everybody. A per-user setting is a settings screen, a column and
    // a migration for a number nobody has asked to change yet; move it there when someone does.
    private const int IdleTimeoutMinutes = 15;

    [Inject] private IStretchedKeyStore KeyStore { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    /// <summary>The routed page, rendered only once the session holds its encryption key.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private readonly UnlockModel _model = new();
    private string? _error;
    private bool _busy;
    private DotNetObjectReference<Unlock>? _selfRef;

    // This component wraps every routed page, so it is the one place the idle watch can be
    // started once and never restarted on navigation.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _selfRef = DotNetObjectReference.Create(this);
        await Js.InvokeVoidAsync("startIdleWatch", _selfRef,
            (int)TimeSpan.FromMinutes(IdleTimeoutMinutes).TotalMilliseconds);
    }

    /// <summary>Called from JS when the idle timeout expires. Locking an already-locked session
    /// is a no-op, so the watch runs unconditionally instead of being started and stopped.</summary>
    [JSInvokable]
    public Task OnIdle()
    {
        AuthService.Lock();
        return InvokeAsync(StateHasChanged);
    }

    private async Task HandleSubmit()
    {
        // The form has no validator, so an empty submit reaches the KDF, which rejects an empty
        // password with an ArgumentException — an unhandled crash on a plain "pressed Unlock too
        // early". Cheaper to refuse it here than to hang a DataAnnotations model off one field.
        if (string.IsNullOrEmpty(_model.Password))
        {
            _error = "Enter your master password.";
            return;
        }

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

    public async ValueTask DisposeAsync()
    {
        if (_selfRef is null)
        {
            return;
        }

        try
        {
            await Js.InvokeVoidAsync("stopIdleWatch");
        }
        catch (JSDisconnectedException)
        {
            // The page is already gone; there is nothing left to unhook.
        }

        _selfRef.Dispose();
    }

    private sealed class UnlockModel
    {
        public string Password { get; set; } = string.Empty;
    }
}
