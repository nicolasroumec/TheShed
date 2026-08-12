using Microsoft.AspNetCore.Components;
using TheShed.Client.Services;
using TheShed.Shared.Models.DTOs.Vaults;
using TheShed.Shared.Models.Enums;

namespace TheShed.Client.Components.Vault;

public partial class MembersPanel
{
    [Parameter] public int VaultId { get; set; }

    [Inject] private VaultClient VaultApi { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

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
            var error = await VaultApi.AddMemberAsync(VaultId, new VaultMemberAddRequest
            {
                Email = _memberEmail.Trim(),
                Role = _memberRole
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
