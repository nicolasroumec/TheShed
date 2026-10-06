namespace TheShed.Shared.Models.DTOs.Auth
{
    /// <summary>The caller's own wrap of a vault they own — the one wrapped with the stretched
    /// master key (members' wraps use RSA instead). A master password change has to re-wrap every
    /// one of these, trashed vaults included, or they become undecryptable.</summary>
    public class OwnedVaultKey
    {
        public int VaultId { get; set; }
        public string WrappedKey { get; set; } = string.Empty;
    }
}
