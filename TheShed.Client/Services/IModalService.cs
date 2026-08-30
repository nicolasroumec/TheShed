namespace TheShed.Client.Services
{
    /// <param name="RequiresPassword">Renders a master-password field and resolves with what was
    /// typed, instead of resolving with a plain yes/no.</param>
    public record ModalRequest(string Title, string Message, string ConfirmText, string ConfirmVariant, bool RequiresPassword = false);

    public interface IModalService
    {
        Task<bool> ConfirmAsync(string message, string title = "Confirm", string confirmText = "Confirm", string confirmVariant = "danger");

        /// <summary>
        /// Asks for the master password. Returns what was typed, or null if cancelled. The dialog
        /// stays open in a busy state after the answer, because verifying it costs a ~1s key
        /// derivation: the caller either prompts again (a wrong answer) or calls
        /// <see cref="Close"/> (accepted).
        /// </summary>
        Task<string?> PromptPasswordAsync(string message, string title = "Confirm it's you", string confirmText = "Confirm");

        /// <summary>Dismisses a password prompt left open and busy by
        /// <see cref="PromptPasswordAsync"/>.</summary>
        void Close();
    }
}
