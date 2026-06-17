using Microsoft.JSInterop;

namespace TheShed.Client.Services
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _js;

        public LocalStorageService(IJSRuntime js) => _js = js;

        public ValueTask<string?> GetItemAsync(string key) =>
            _js.InvokeAsync<string?>("localStorage.getItem", key);

        public ValueTask SetItemAsync(string key, string value) =>
            _js.InvokeVoidAsync("localStorage.setItem", key, value);

        public ValueTask RemoveItemAsync(string key) =>
            _js.InvokeVoidAsync("localStorage.removeItem", key);
    }
}
