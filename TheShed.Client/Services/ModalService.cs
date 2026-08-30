namespace TheShed.Client.Services
{
    /// <summary>Backs the app's single modal host (see Components/ModalHost) so callers — even
    /// from pure C# with no markup of their own — can await a themed confirm dialog instead of
    /// the browser's native window.confirm.</summary>
    public class ModalService : IModalService
    {
        public event Action<ModalRequest>? OnShow;
        public event Action? OnClose;

        // One pending result for both shapes: a confirm resolves with an empty string for yes,
        // a password prompt with what was typed, and either resolves null when cancelled.
        private TaskCompletionSource<string?>? _pending;

        public async Task<bool> ConfirmAsync(string message, string title = "Confirm", string confirmText = "Confirm", string confirmVariant = "danger") =>
            await ShowAsync(new ModalRequest(title, message, confirmText, confirmVariant)) is not null;

        public Task<string?> PromptPasswordAsync(string message, string title = "Confirm it's you", string confirmText = "Confirm") =>
            ShowAsync(new ModalRequest(title, message, confirmText, "primary", RequiresPassword: true));

        private Task<string?> ShowAsync(ModalRequest request)
        {
            _pending = new TaskCompletionSource<string?>();
            OnShow?.Invoke(request);
            return _pending.Task;
        }

        public void Resolve(string? result) => _pending?.TrySetResult(result);

        public void Close() => OnClose?.Invoke();
    }
}
