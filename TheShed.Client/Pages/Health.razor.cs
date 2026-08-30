using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Helpers;
using TheShed.Shared.Models.DTOs.Health;
using TheShed.Shared.Security;

namespace TheShed.Client.Pages;

public partial class Health
{
    [Inject] private VaultClient VaultApi { get; set; } = default!;
    [Inject] private EntryClient EntryApi { get; set; } = default!;
    [Inject] private IVaultKeyResolver KeyResolver { get; set; } = default!;
    [Inject] private IAesGcmService AesGcm { get; set; } = default!;

    private IReadOnlyList<PasswordHealthItem>? _items;
    private int _unreadableVaults;

    protected override async Task OnInitializedAsync()
    {
        // Decrypted only in memory to rate/compare; never persisted or logged. Moved client-side
        // in Sprint 27 — the server has been unable to decrypt PasswordEntry.Password at all
        // since Sprint 26, so the old server-side report was silently broken until this.
        var decrypted = new List<(int EntryId, int VaultId, string EntryName, string Password)>();

        foreach (var vaultItem in await VaultApi.ListAsync())
        {
            var vault = await VaultApi.GetAsync(vaultItem.Id);
            if (vault is null)
            {
                _unreadableVaults++;
                continue;
            }

            // No key for this vault — a member whose VaultKeyWrap is missing, or one shared before
            // Sprint 27. Sprint 30's unlock prompt covers the common cause (a reload wiping the
            // in-memory keys), so what is left here is rare but not impossible. Counted rather than
            // swallowed: a skipped vault does not just shorten the report, it corrupts it, because
            // reuse is compared across every vault at once.
            var vaultKey = await KeyResolver.ResolveAsync(vault);
            if (vaultKey is null)
            {
                _unreadableVaults++;
                continue;
            }

            foreach (var entry in await EntryApi.ListAsync(vault.Id))
            {
                var detail = await EntryApi.GetAsync(entry.Id);
                if (detail is null)
                {
                    continue;
                }

                var name = await AesGcm.DecryptAsync(vaultKey, detail.Name);
                var password = await AesGcm.DecryptAsync(vaultKey, detail.Password);
                decrypted.Add((entry.Id, vault.Id, name, password));
            }
        }

        var reused = decrypted
            .GroupBy(e => e.Password)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        _items = decrypted
            .Select(e => new PasswordHealthItem
            {
                EntryId = e.EntryId,
                VaultId = e.VaultId,
                EntryName = e.EntryName,
                Strength = PasswordHealthChecker.EvaluateStrength(e.Password),
                IsReused = reused.Contains(e.Password)
            })
            .OrderBy(i => i.EntryName)
            .ToList();
    }

    private static string StrengthChipClass(PasswordStrength strength) => strength switch
    {
        PasswordStrength.Weak => "strength-weak",
        PasswordStrength.Medium => "strength-medium",
        _ => "strength-strong"
    };
}
