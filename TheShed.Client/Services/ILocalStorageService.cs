namespace TheShed.Client.Services
{
    public interface ILocalStorageService
    {
        ValueTask<string?> GetItemAsync(string key);
        ValueTask SetItemAsync(string key, string value);
        ValueTask RemoveItemAsync(string key);
    }
}
