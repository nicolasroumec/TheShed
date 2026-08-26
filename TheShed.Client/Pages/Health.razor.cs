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
                continue;
            }

            // No key available this session (e.g. a page reload wiped the in-memory stretched
            // key/keypair) — skip rather than show a report that's silently missing a vault.
            var vaultKey = await KeyResolver.ResolveAsync(vault);
            if (vaultKey is null)
            {
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
