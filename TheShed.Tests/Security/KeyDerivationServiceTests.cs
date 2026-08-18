using TheShed.Shared.Security;

namespace TheShed.Tests.Security
{
    public class KeyDerivationServiceTests
    {
        private readonly KeyDerivationService _sut = new();

        [Fact]
        public void DeriveKey_SamePasswordAndSalt_ReturnsSameKey()
        {
            var salt = _sut.GenerateSalt();
            var key1 = _sut.DeriveKey("correct horse battery staple", salt);
            var key2 = _sut.DeriveKey("correct horse battery staple", salt);
            Assert.Equal(key1, key2);
        }

        [Fact]
        public void DeriveKey_DifferentSalt_ReturnsDifferentKey()
        {
            var key1 = _sut.DeriveKey("same password", _sut.GenerateSalt());
            var key2 = _sut.DeriveKey("same password", _sut.GenerateSalt());
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void DeriveKey_ReturnsThirtyTwoBytes()
        {
            var key = _sut.DeriveKey("password", _sut.GenerateSalt());
            Assert.Equal(32, key.Length);
        }
    }
}
