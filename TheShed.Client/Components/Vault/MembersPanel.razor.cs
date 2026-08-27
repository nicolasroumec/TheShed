using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Vaults;
using TheShed.Shared.Models.Enums;
using TheShed.Shared.Security;

namespace TheShed.Client.Components.Vault;

public partial class MembersPanel
{
    [Parameter] public int VaultId { get; set; }

    [Inject] private VaultClient VaultApi { get; set; } = default!;
    [Inject] private UserClient UserApi { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] private IVaultKeyCache VaultKeyCache { get; set; } = default!;
    [Inject] private IUserKeypairService UserKeypair { get; set; } = default!;

    private IReadOnlyList<VaultMemberItem>? _members;
    private string _memberEmail = string.Empty;
    private VaultRole _memberRole = VaultRole.Viewer;
    private bool _memberBusy;
    private string? _memberError;

    protected override async Task OnInitializedAsync()
    {
        _members = await VaultApi.ListMembersAsync(VaultId);
    }

    private async Task AddMemberAsync()
    {
        _memberError = null;
        if (string.IsNullOrWhiteSpace(_memberEmail))
        {
            return;
        }

        _memberBusy = true;
        try
        {
            var vaultKey = VaultKeyCache.Get(VaultId);
            if (vaultKey is null)
            {
                _memberError = "Vault key unavailable — reopen the vault and try again.";
                return;
            }

            var email = _memberEmail.Trim();
            var target = await UserApi.GetPublicKeyAsync(email);
            if (target is null)
            {
                _memberError = "No user with that email, or they haven't set up sharing yet.";
                return;
            }

            var wrappedKey = await UserKeypair.WrapKeyForMemberAsync(vaultKey, target.PublicKey);

            var error = await VaultApi.AddMemberAsync(VaultId, new VaultMemberAddRequest
            {
                Email = email,
                Role = _memberRole,
                VaultKeyWrap = wrappedKey
            });

            if (error is not null)
            {
                _memberError = error;
                return;
            }

            _memberEmail = string.Empty;
            _memberRole = VaultRole.Viewer;
            _members = await VaultApi.ListMembersAsync(VaultId);
        }
        finally
        {
            _memberBusy = false;
        }
    }

    private async Task ChangeRoleAsync(VaultMemberItem member, ChangeEventArgs e)
    {
        var role = Enum.Parse<VaultRole>((string)e.Value!);
        await VaultApi.UpdateMemberRoleAsync(VaultId, member.UserId, new VaultMemberRoleUpdateRequest { Role = role });
        _members = await VaultApi.ListMembersAsync(VaultId);
    }

    private async Task RemoveMemberAsync(VaultMemberItem member)
    {
        if (!await Modal.ConfirmAsync($"Remove {member.Username}'s access to this vault?", title: "Remove member", confirmText: "Remove"))
        {
            return;
        }

        _memberError = null;
        try
        {
            await VaultApi.RemoveMemberAsync(VaultId, member.UserId);
            _members = await VaultApi.ListMembersAsync(VaultId);
        }
        catch (Exception)
        {
            _memberError = $"Could not remove {member.Username}. Please try again.";
        }
    }
}
