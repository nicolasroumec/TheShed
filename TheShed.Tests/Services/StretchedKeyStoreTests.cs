using TheShed.Client.Services;
using Xunit;

namespace TheShed.Tests.Services
{
    public class StretchedKeyStoreTests
    {
        [Fact]
        public void Get_SinSet_DevuelveNull()
        {
            var store = new StretchedKeyStore();
            Assert.Null(store.Get());
        }

        [Fact]
        public void Set_LuegoGet_DevuelveLaMismaKey()
        {
            var store = new StretchedKeyStore();
            var key = new byte[] { 1, 2, 3 };

            store.Set(key);

            Assert.Same(key, store.Get());
        }

        [Fact]
        public void Clear_DespuesDeSet_VuelveADevolverNull()
        {
            var store = new StretchedKeyStore();
            store.Set(new byte[] { 1, 2, 3 });

            store.Clear();

            Assert.Null(store.Get());
        }
    }
}
