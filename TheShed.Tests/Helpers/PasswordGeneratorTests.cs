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

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(8)]
        public void GeneratePassphrase_HasRequestedWordCount(int wordCount)
        {
            var passphrase = PasswordGenerator.GeneratePassphrase(wordCount);

            Assert.Equal(wordCount, passphrase.Split('-').Length);
        }

        [Fact]
        public void GeneratePassphrase_UsesGivenSeparator()
        {
            var passphrase = PasswordGenerator.GeneratePassphrase(3, separator: "_");

            Assert.Equal(3, passphrase.Split('_').Length);
            Assert.DoesNotContain("-", passphrase);
        }

        [Fact]
        public void GeneratePassphrase_CapitalizeFirst_CapitalizesEachWord()
        {
            var passphrase = PasswordGenerator.GeneratePassphrase(4, capitalizeFirst: true);

            Assert.All(passphrase.Split('-'), word => Assert.True(char.IsUpper(word[0])));
        }

        [Fact]
        public void GeneratePassphrase_WithoutCapitalize_IsLowercase()
        {
            var passphrase = PasswordGenerator.GeneratePassphrase(4);

            Assert.Equal(passphrase, passphrase.ToLowerInvariant());
        }

        [Fact]
        public void GeneratePassphrase_IncludeNumber_AppendsTrailingDigit()
        {
            var passphrase = PasswordGenerator.GeneratePassphrase(3, includeNumber: true);
            var last = passphrase.Split('-')[^1];

            Assert.Single(last);
            Assert.True(char.IsDigit(last[0]));
        }

        [Fact]
        public void GeneratePassphrase_ZeroWords_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.GeneratePassphrase(0));
        }

        [Fact]
        public void GeneratePassphrase_IsRandom()
        {
            Assert.NotEqual(PasswordGenerator.GeneratePassphrase(6), PasswordGenerator.GeneratePassphrase(6));
        }
    }
}
