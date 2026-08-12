namespace TheShed.Client.Services
{
    /// <summary>Backs the app's single modal host (see Components/ModalHost) so callers — even
    /// from pure C# with no markup of their own — can await a themed confirm dialog instead of
    /// the browser's native window.confirm.</summary>
    public class ModalService : IModalService
    {
        public event Action<ModalRequest>? OnShow;

        private TaskCompletionSource<bool>? _pending;

        public Task<bool> ConfirmAsync(string message, string title = "Confirm", string confirmText = "Confirm", string confirmVariant = "danger")
        {
            _pending = new TaskCompletionSource<bool>();
            OnShow?.Invoke(new ModalRequest(title, message, confirmText, confirmVariant));
            return _pending.Task;
        }

        public void Resolve(bool result) => _pending?.TrySetResult(result);
    }
}
