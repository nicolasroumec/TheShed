using TheShed.Server.Enums;
using TheShed.Shared.Models.DTOs.Vaults;

namespace TheShed.Server.Services
{
    public interface IVaultService
    {
        // ponytail: reuses EntryResult<T>/EntryError (generic None/NotFound/Forbidden
        // outcome). Rename both to ServiceResult/ServiceError when a third consumer lands.
        Task<IReadOnlyList<VaultListItem>> ListAsync(int userId, CancellationToken ct = default);
        Task<EntryResult<VaultResponse>> GetAsync(int userId, int vaultId, CancellationToken ct = default);
        Task<VaultResponse> CreateAsync(int userId, VaultCreateRequest request, CancellationToken ct = default);
        Task<EntryResult<VaultResponse>> UpdateAsync(int userId, int vaultId, VaultUpdateRequest request, CancellationToken ct = default);
        Task<EntryResult<bool>> DeleteAsync(int userId, int vaultId, CancellationToken ct = default);

        // Member management — owner-only.
        Task<EntryResult<IReadOnlyList<VaultMemberItem>>> ListMembersAsync(int userId, int vaultId, CancellationToken ct = default);
        Task<EntryResult<VaultMemberItem>> AddMemberAsync(int userId, int vaultId, VaultMemberAddRequest request, CancellationToken ct = default);
        Task<EntryResult<VaultMemberItem>> UpdateMemberRoleAsync(int userId, int vaultId, int memberUserId, VaultMemberRoleUpdateRequest request, CancellationToken ct = default);
        Task<EntryResult<bool>> RemoveMemberAsync(int userId, int vaultId, int memberUserId, CancellationToken ct = default);
    }
}
