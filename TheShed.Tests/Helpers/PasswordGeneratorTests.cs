using TheShed.Shared.Helpers;
using Xunit;

namespace TheShed.Tests.Helpers
{
    public class PasswordGeneratorTests
    {
        [Theory]
        [InlineData(8)]
        [InlineData(20)]
        [InlineData(64)]
        public void Generate_RespectsLength(int length)
        {
            Assert.Equal(length, PasswordGenerator.Generate(length).Length);
        }

        [Fact]
        public void Generate_IncludesEveryRequiredClass()
        {
            var pwd = PasswordGenerator.Generate(20, includeSymbols: true);

            Assert.Contains(pwd, char.IsLower);
            Assert.Contains(pwd, char.IsUpper);
            Assert.Contains(pwd, char.IsDigit);
            Assert.Contains(pwd, c => !char.IsLetterOrDigit(c)); // a symbol
        }

        [Fact]
        public void Generate_WithoutSymbols_IsAlphanumeric()
        {
            var pwd = PasswordGenerator.Generate(20, includeSymbols: false);

            Assert.All(pwd, c => Assert.True(char.IsLetterOrDigit(c)));
        }

        [Fact]
        public void Generate_TooShortForClasses_Throws()
        {
            // 4 classes (incl. symbols) need length >= 4.
            Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.Generate(3, includeSymbols: true));
        }

        [Fact]
        public void Generate_IsRandom()
        {
            Assert.NotEqual(PasswordGenerator.Generate(), PasswordGenerator.Generate());
        }
    }
}
