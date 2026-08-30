using System.Net;
using System.Security.Cryptography;
using System.Text;
using TheShed.Client.Services;
using TheShed.Shared.Security;
using Xunit;

namespace TheShed.Tests.Services
{
    /// <summary>Sprint 30, Increment 4: reauthentication before a plaintext password is shown or
    /// copied (AUDITORIA A1).</summary>
    public class ReauthGateTests
    {
        private const string CorrectPassword = "hunter2";

        [Fact]
        public async Task EnsureAsync_AsksOnceAndThenStaysOpenForTheWindow()
        {
            var (gate, modal) = Build(locked: false, CorrectPassword);

            Assert.True(await gate.EnsureAsync());
            Assert.True(await gate.EnsureAsync());

            // The point of the window: revealing three entries in a row is one prompt, not three.
            Assert.Equal(1, modal.Prompts);
        }

        [Fact]
        public async Task EnsureAsync_WithAWrongPasswordFirst_AsksAgainRatherThanFailing()
        {
            var (gate, modal) = Build(locked: false, "wrong-one", CorrectPassword);

            Assert.True(await gate.EnsureAsync());
            Assert.Equal(2, modal.Prompts);
        }

        [Fact]
        public async Task EnsureAsync_WhenCancelled_DeniesAndOpensNoWindow()
        {
            var (gate, modal) = Build(locked: false, null, CorrectPassword);

            Assert.False(await gate.EnsureAsync());
            // A cancelled prompt must not count as a confirmation: the next attempt asks again.
            Assert.True(await gate.EnsureAsync());
            Assert.Equal(2, modal.Prompts);
        }

        /// <summary>Regression: with no key to compare against, every answer looks wrong, so the
        /// dialog used to insist the right master password was the wrong one, forever.</summary>
        [Fact]
        public async Task EnsureAsync_WhenTheSessionIsLocked_DeniesWithoutPrompting()
        {
            var (gate, modal) = Build(locked: true, CorrectPassword);

            Assert.False(await gate.EnsureAsync());
            Assert.Equal(0, modal.Prompts);
        }

        private static (ReauthGate Gate, FakeModal Modal) Build(bool locked, params string?[] answers)
        {
            var body = """{"username":"nico","email":"nico@example.com","keySalt":"c2FsdHktc2FsdC0xMjM0"}""";
            var http = new HttpClient(new StubHandler(body)) { BaseAddress = new Uri("https://localhost/") };

            var keyStore = new StretchedKeyStore();
            if (!locked)
            {
                keyStore.Set(FakeKdf.KeyFor(CorrectPassword));
            }

            var modal = new FakeModal(answers);
            return (new ReauthGate(http, modal, new FakeKdf(), keyStore), modal);
        }

        private sealed class StubHandler(string body) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
        }

        private sealed class FakeModal(string?[] answers) : IModalService
        {
            private int _next;

            public int Prompts { get; private set; }

            public Task<bool> ConfirmAsync(string message, string title = "Confirm", string confirmText = "Confirm", string confirmVariant = "danger") =>
                throw new InvalidOperationException("the gate prompts for a password, never a plain confirm");

            public Task<string?> PromptPasswordAsync(string message, string title = "Confirm it's you", string confirmText = "Confirm")
            {
                Prompts++;
                return Task.FromResult(answers[_next++]);
            }
        }

        private sealed class FakeKdf : IKeyDerivationService
        {
            public static byte[] KeyFor(string password) => SHA256.HashData(Encoding.UTF8.GetBytes(password));

            public byte[] GenerateSalt() => new byte[16];

            public Task<byte[]> DeriveKeyAsync(string password, byte[] salt) => Task.FromResult(KeyFor(password));
        }
    }
}
