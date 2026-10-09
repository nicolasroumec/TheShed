using TheShed.Client.Services;
using Xunit;

namespace TheShed.Tests.Services
{
    public class VaultKeyCacheTests
    {
        [Fact]
        public void Get_WithoutSet_ReturnsNull()
        {
            var cache = new VaultKeyCache();
            Assert.Null(cache.Get(1));
        }

        [Fact]
        public void Set_ThenGet_ReturnsThatVaultsKey()
        {
            var cache = new VaultKeyCache();
            var key = new byte[] { 1, 2, 3 };

            cache.Set(1, key);

            Assert.Same(key, cache.Get(1));
            Assert.Null(cache.Get(2)); // another vault, no key of its own
        }

        [Fact]
        public void Clear_BorraTodasLasKeys()
        {
            var cache = new VaultKeyCache();
            cache.Set(1, new byte[] { 1 });
            cache.Set(2, new byte[] { 2 });

            cache.Clear();

            Assert.Null(cache.Get(1));
            Assert.Null(cache.Get(2));
        }
    }
}
