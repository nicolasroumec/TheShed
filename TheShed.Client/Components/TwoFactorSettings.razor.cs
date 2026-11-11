using Microsoft.AspNetCore.Components;
using QRCoder;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Auth;

namespace TheShed.Client.Components;

/// <summary>Account section to turn TOTP 2FA on and off (Sprint 18). The QR is rendered in the
/// browser, so the secret never travels in any request besides the setup response.</summary>
public partial class TwoFactorSettings
{
    [Inject] private TwoFactorClient TwoFactor { get; set; } = default!;

    private bool? _enabled;
    private TwoFactorSetupResponse? _setup;
    private string? _qrDataUri;
    private string _code = string.Empty;
    private string? _error;
    private bool _busy;

    protected override async Task OnInitializedAsync() => _enabled = await TwoFactor.IsEnabledAsync();

    private async Task BeginSetup()
    {
        await Run(async () =>
        {
            _setup = await TwoFactor.BeginSetupAsync();
            if (_setup is null)
            {
                // Turned on from another tab or device meanwhile.
                _enabled = await TwoFactor.IsEnabledAsync();
                return;
            }
            using var qr = QRCodeGenerator.GenerateQrCode(_setup.OtpAuthUri, QRCodeGenerator.ECCLevel.M);
            _qrDataUri = "data:image/png;base64," + Convert.ToBase64String(new PngByteQRCode(qr).GetGraphic(8));
        });
    }

    private void CancelSetup()
    {
        // The pending secret stays on the server unused; the next setup replaces it.
        _setup = null;
        _qrDataUri = null;
        _code = string.Empty;
        _error = null;
    }

    private async Task Enable() => await Run(async () =>
    {
        if (await TwoFactor.EnableAsync(_code.Trim()))
        {
            _enabled = true;
            CancelSetup();
        }
        else
        {
            _error = "That code didn't match. Check your phone's clock and try the next code.";
        }
    });

    private async Task Disable() => await Run(async () =>
    {
        if (await TwoFactor.DisableAsync(_code.Trim()))
        {
            _enabled = false;
            _code = string.Empty;
        }
        else
        {
            _error = "That code didn't match. Wait for the next one and try again.";
        }
    });

    private async Task Run(Func<Task> action)
    {
        _busy = true;
        _error = null;
        try
        {
            await action();
        }
        catch (HttpRequestException)
        {
            _error = "Unexpected error. Please try again.";
        }
        finally
        {
            _busy = false;
        }
    }
}
