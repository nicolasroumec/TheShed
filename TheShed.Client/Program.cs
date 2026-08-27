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

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

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
builder.Services.AddScoped<IKeyDerivationService, WebCryptoKeyDerivationService>();
builder.Services.AddScoped<IUserKeypairService, WebCryptoUserKeypairService>();
builder.Services.AddScoped<IStretchedKeyStore, StretchedKeyStore>();
builder.Services.AddScoped<IVaultKeyService, WebCryptoVaultKeyService>();
builder.Services.AddScoped<IVaultKeyCache, VaultKeyCache>();
builder.Services.AddScoped<IOwnKeypairCache, OwnKeypairCache>();
builder.Services.AddScoped<IVaultKeyResolver, VaultKeyResolver>();
builder.Services.AddScoped<IAesGcmService, WebCryptoAesGcmService>();

await builder.Build().RunAsync();
