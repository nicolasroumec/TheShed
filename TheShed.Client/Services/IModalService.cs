namespace TheShed.Client.Services
{
    public record ModalRequest(string Title, string Message, string ConfirmText, string ConfirmVariant);

    public interface IModalService
    {
        Task<bool> ConfirmAsync(string message, string title = "Confirm", string confirmText = "Confirm", string confirmVariant = "danger");
    }
}
