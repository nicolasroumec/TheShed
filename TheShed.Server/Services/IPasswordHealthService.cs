using TheShed.Shared.Models.DTOs.Health;

namespace TheShed.Server.Services
{
    public interface IPasswordHealthService
    {
        /// <summary>Strength + reuse verdict for every entry across the vaults the user
        /// can see (owned or shared). Always succeeds; empty when the user has no entries.</summary>
        Task<IReadOnlyList<PasswordHealthItem>> GetReportAsync(int userId, CancellationToken ct = default);
    }
}
