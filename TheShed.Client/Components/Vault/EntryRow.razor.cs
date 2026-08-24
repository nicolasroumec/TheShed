using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Security;

namespace TheShed.Client.Components.Vault;

public partial class EntryRow
{
    [Parameter, EditorRequired] public EntryListItem Entry { get; set; } = default!;
    [Parameter] public bool CanWrite { get; set; }
    [Parameter] public byte[]? VaultKey { get; set; }
    [Parameter] public EventCallback<EntryListItem> OnFavoriteToggled { get; set; }
    [Parameter] public EventCallback<int> OnDeleteRequested { get; set; }
    [Parameter] public EventCallback<int> OnEditRequested { get; set; }

    [Inject] private EntryClient EntryApi { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] private IAesGcmService AesGcm { get; set; } = default!;

    private const string MissingVaultKeyError = "Vault key unavailable — log out and log back in.";

    private static readonly TimeSpan CopiedLabelDuration = TimeSpan.FromSeconds(2);

    private string? _revealedPassword;
    private string? _actionError;
    private bool _copied;
    private DateTime? _lastPasswordChangedAt;

    private bool _historyOpen;
    private bool _attachmentsOpen;

    protected override void OnParametersSet()
    {
        // The password may have changed under us (an edit in the parent's form). Close the
        // panels so the user has to reopen (and refetch) instead of seeing stale data.
        if (_lastPasswordChangedAt is not null && _lastPasswordChangedAt != Entry.PasswordChangedAt)
        {
            _revealedPassword = null;
            _historyOpen = false;
        }
        _lastPasswordChangedAt = Entry.PasswordChangedAt;
    }

    private static string DaysAgoText(DateTime changedAt)
    {
        var days = (int)(DateTime.UtcNow - changedAt).TotalDays;
        return days switch
        {
            0 => "today",
            1 => "1 day ago",
            _ => $"{days} days ago"
        };
    }

    private async Task ToggleRevealAsync()
    {
        if (_revealedPassword is not null)
        {
            _revealedPassword = null;
            return;
        }

        if (VaultKey is null)
        {
            _actionError = MissingVaultKeyError;
            return;
        }

        var entry = await EntryApi.GetAsync(Entry.Id);
        if (entry is not null)
        {
            _actionError = null;
            _revealedPassword = await AesGcm.DecryptAsync(VaultKey, entry.Password);
        }
    }

    private async Task CopyPasswordAsync()
    {
        if (VaultKey is null)
        {
            _actionError = MissingVaultKeyError;
            return;
        }

        var entry = await EntryApi.GetAsync(Entry.Id);
        if (entry is null)
        {
            return;
        }

        var plaintext = await AesGcm.DecryptAsync(VaultKey, entry.Password);
        await JS.InvokeVoidAsync("copyToClipboard", plaintext);

        // Flash "Copied!" and put the button back. StateHasChanged is needed here because the
        // label has to update before the delay, not after the handler finally returns.
        _actionError = null;
        _copied = true;
        StateHasChanged();
        await Task.Delay(CopiedLabelDuration);
        _copied = false;
    }

    private async Task DeleteAsync()
    {
        if (!await Modal.ConfirmAsync("Delete this entry? This cannot be undone.", title: "Delete entry", confirmText: "Delete"))
        {
            return;
        }

        await OnDeleteRequested.InvokeAsync(Entry.Id);
    }
}
