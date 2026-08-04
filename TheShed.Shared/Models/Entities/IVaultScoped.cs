namespace TheShed.Shared.Models.Entities
{
    /// <summary>An auditable entity that belongs to a single vault (entries, notes).
    /// Lets trash restore/purge share one generic implementation across both.</summary>
    public interface IVaultScoped
    {
        int Id { get; }
        int VaultId { get; }
    }
}
