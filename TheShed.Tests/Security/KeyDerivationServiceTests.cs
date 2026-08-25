using TheShed.Shared.Security;

namespace TheShed.Tests.Security
{
    public class KeyDerivationServiceTests
    {
        private readonly KeyDerivationService _sut = new();

        [Fact]
        public async Task DeriveKey_SamePasswordAndSalt_ReturnsSameKey()
        {
            var salt = _sut.GenerateSalt();
            var key1 = await _sut.DeriveKeyAsync("correct horse battery staple", salt);
            var key2 = await _sut.DeriveKeyAsync("correct horse battery staple", salt);
            Assert.Equal(key1, key2);
        }

        [Fact]
        public async Task DeriveKey_DifferentSalt_ReturnsDifferentKey()
        {
            var key1 = await _sut.DeriveKeyAsync("same password", _sut.GenerateSalt());
            var key2 = await _sut.DeriveKeyAsync("same password", _sut.GenerateSalt());
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public async Task DeriveKey_ReturnsThirtyTwoBytes()
        {
            var key = await _sut.DeriveKeyAsync("password", _sut.GenerateSalt());
            Assert.Equal(32, key.Length);
        }
    }
}
