using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TheShed.Client;
using TheShed.Client.Auth;
using TheShed.Client.Services;
using TheShed.Shared.Security;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The CsrfHandler intercepts every request made through this HttpClient and attaches the
// antiforgery header (A4) to mutating ones — every typed client below shares it, so none of
// them need to know the token exists. Must be a singleton: IHttpClientFactory resolves
// AddHttpMessageHandler<T> instances from its own internal per-handler-lifetime DI scope, so a
// Scoped registration here would hand the HTTP pipeline a different instance than the one
// AuthService injects and calls Invalidate() on — the cached token would never actually clear.
builder.Services.AddSingleton<CsrfHandler>();
builder.Services.AddHttpClient("Default", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<CsrfHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ModalService>();
builder.Services.AddScoped<IModalService>(sp => sp.GetRequiredService<ModalService>());
builder.Services.AddScoped<VaultClient>();
builder.Services.AddScoped<UserClient>();
builder.Services.AddScoped<EntryClient>();
builder.Services.AddScoped<NoteClient>();
builder.Services.AddScoped<TagClient>();
builder.Services.AddScoped<TrashClient>();
builder.Services.AddScoped<AttachmentClient>();
builder.Services.AddScoped<ImportExportClient>();
builder.Services.AddScoped<IKeyDerivationService, WebCryptoKeyDerivationService>();
builder.Services.AddScoped<IUserKeypairService, WebCryptoUserKeypairService>();
builder.Services.AddScoped<IStretchedKeyStore, StretchedKeyStore>();
builder.Services.AddScoped<IVaultKeyService, WebCryptoVaultKeyService>();
builder.Services.AddScoped<IVaultKeyCache, VaultKeyCache>();
builder.Services.AddScoped<IOwnKeypairCache, OwnKeypairCache>();
builder.Services.AddScoped<IVaultKeyResolver, VaultKeyResolver>();
builder.Services.AddScoped<IReauthGate, ReauthGate>();
builder.Services.AddScoped<IAesGcmService, WebCryptoAesGcmService>();

await builder.Build().RunAsync();
