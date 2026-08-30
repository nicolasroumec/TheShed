using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Entries;
using TheShed.Shared.Security;

namespace TheShed.Client.Components.Vault;

public partial class EntryHistoryPanel
{
    [Parameter, EditorRequired] public int EntryId { get; set; }
    [Parameter] public DateTime PasswordChangedAt { get; set; }
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public byte[]? VaultKey { get; set; }

    [Inject] private EntryClient EntryApi { get; set; } = default!;
    [Inject] private IAesGcmService AesGcm { get; set; } = default!;
    [Inject] private IReauthGate Reauth { get; set; } = default!;

    private IReadOnlyList<EntryHistoryItem>? _history;
    private readonly Dictionary<int, string> _revealedHistory = new(); // historyId -> decrypted old password
    private string? _actionError;
    private DateTime? _lastPasswordChangedAt;

    protected override async Task OnParametersSetAsync()
    {
        // The password may have changed under us (an edit in the parent's form). A stale
        // history cache would otherwise keep showing pre-edit entries as "current".
        if (_lastPasswordChangedAt is not null && _lastPasswordChangedAt != PasswordChangedAt)
        {
            _history = null;
            _revealedHistory.Clear();
        }
        _lastPasswordChangedAt = PasswordChangedAt;

        if (IsOpen && _history is null)
        {
            _history = await EntryApi.GetHistoryAsync(EntryId);
        }
    }

    private async Task ToggleHistoryRevealAsync(int historyId)
    {
        if (_revealedHistory.Remove(historyId))
        {
            return; // was shown, now hidden
        }

        if (VaultKey is null)
        {
            _actionError = "Vault key unavailable — log out and log back in.";
            return;
        }

        // An old password is as sensitive as the current one: people reuse them elsewhere.
        if (!await Reauth.EnsureAsync())
        {
            return;
        }

        var detail = await EntryApi.GetHistoryEntryAsync(EntryId, historyId);
        if (detail is not null)
        {
            _actionError = null;
            _revealedHistory[historyId] = await AesGcm.DecryptAsync(VaultKey, detail.Password);
        }
    }
}
