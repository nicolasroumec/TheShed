using System.Net;
using System.Net.Http.Json;
using TheShed.Shared.Models.DTOs.Vaults;
using TheShed.Shared.Models.Enums;
using Xunit;

namespace TheShed.Tests.Integration
{
    /// <summary>Cross-cutting authorization on vaults/entries against the real pipeline
    /// (VaultAccessService + EF, not a mock standing in for the access decision). A controller
    /// test with IVaultAccessService mocked can't catch a bug here — mocking the response
    /// assumes the access logic is already correct, which is exactly what needs testing.</summary>
    public class VaultAccessTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public VaultAccessTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Dueno_PuedeLeerYEscribirSuPropiaVault()
        {
            var owner = _factory.CreateAuthenticatedClient();
            await owner.RegisterNewUserAsync();
            var vaultId = await CreateVaultAsync(owner, "Vault del dueño");

            var get = await owner.GetAsync($"api/vaults/{vaultId}");
            get.EnsureSuccessStatusCode();

            var create = await owner.PostJsonWithCsrfAsync("api/entries", EntryPayload(vaultId));
            create.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task UsuarioAjeno_RecibeNotFoundAlLeerOEscribirUnaVaultQueNoLeComparten()
        {
            var owner = _factory.CreateAuthenticatedClient();
            await owner.RegisterNewUserAsync();
            var vaultId = await CreateVaultAsync(owner, "Vault privada");

            var outsider = _factory.CreateAuthenticatedClient();
            await outsider.RegisterNewUserAsync();

            var get = await outsider.GetAsync($"api/vaults/{vaultId}");
            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

            var create = await outsider.PostJsonWithCsrfAsync("api/entries", EntryPayload(vaultId));
            Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
        }

        [Fact]
        public async Task MiembroViewer_PuedeLeerPeroNoEscribir()
        {
            var owner = _factory.CreateAuthenticatedClient();
            await owner.RegisterNewUserAsync();
            var vaultId = await CreateVaultAsync(owner, "Vault compartida (viewer)");

            var viewer = _factory.CreateAuthenticatedClient();
            var (viewerEmail, _) = await viewer.RegisterNewUserAsync();
            await AddMemberAsync(owner, vaultId, viewerEmail, VaultRole.Viewer);

            var get = await viewer.GetAsync($"api/vaults/{vaultId}");
            get.EnsureSuccessStatusCode();

            var create = await viewer.PostJsonWithCsrfAsync("api/entries", EntryPayload(vaultId));
            Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        }

        [Fact]
        public async Task MiembroEditor_PuedeEscribir()
        {
            var owner = _factory.CreateAuthenticatedClient();
            await owner.RegisterNewUserAsync();
            var vaultId = await CreateVaultAsync(owner, "Vault compartida (editor)");

            var editor = _factory.CreateAuthenticatedClient();
            var (editorEmail, _) = await editor.RegisterNewUserAsync();
            await AddMemberAsync(owner, vaultId, editorEmail, VaultRole.Editor);

            var create = await editor.PostJsonWithCsrfAsync("api/entries", EntryPayload(vaultId));
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        }

        private static object EntryPayload(int vaultId) => new
        {
            VaultId = vaultId,
            Name = "ciphertext-name",
            Username = "ciphertext-username",
            Password = "ciphertext-password"
        };

        private static async Task<int> CreateVaultAsync(HttpClient owner, string name)
        {
            var response = await owner.PostJsonWithCsrfAsync("api/vaults", new
            {
                Name = name,
                VaultKeyWrap = "wrapped-vault-key"
            });
            response.EnsureSuccessStatusCode();
            var vault = await response.Content.ReadFromJsonAsync<VaultResponse>();
            return vault!.Id;
        }

        private static async Task AddMemberAsync(HttpClient owner, int vaultId, string memberEmail, VaultRole role)
        {
            var response = await owner.PostJsonWithCsrfAsync($"api/vaults/{vaultId}/members", new
            {
                Email = memberEmail,
                Role = role,
                VaultKeyWrap = "wrapped-vault-key-for-member"
            });
            response.EnsureSuccessStatusCode();
        }
    }
}
