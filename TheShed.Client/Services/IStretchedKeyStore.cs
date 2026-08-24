namespace TheShed.Client.Services
{
    /// <summary>
    /// Holds the client-side stretched master key (<see cref="TheShed.Shared.Security.IKeyDerivationService"/>'s
    /// output) in memory for the lifetime of the authenticated session. Never persisted — a page
    /// reload means logging in again, which re-derives it. Backs Sprint 26's vault key wrapping:
    /// the owner's stretched master key wraps each vault's AES key, so it has to be available
    /// after login/register, not just at the moment the password is typed.
    /// </summary>
    public interface IStretchedKeyStore
    {
        void Set(byte[] key);
        byte[]? Get();
        void Clear();
    }
}
