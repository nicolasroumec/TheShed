namespace TheShed.Client.Services
{
    /// <summary>
    /// Gates the actions that put a plaintext password on screen or on the clipboard behind a
    /// fresh master-password confirmation (Sprint 30, closes AUDITORIA A1). Until now a valid JWT
    /// cookie was enough, and that lasts an hour on an unattended machine.
    /// </summary>
    public interface IReauthGate
    {
        /// <summary>
        /// True once the master password has been confirmed within the recent-confirmation
        /// window, prompting for it if it has not. False means the user cancelled.
        /// </summary>
        Task<bool> EnsureAsync();
    }
}
