using TheShed.Shared.Helpers;
using Xunit;

namespace TheShed.Tests.Helpers
{
    public class PasswordHealthCheckerTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("abcdefgh")]
        [InlineData("password")]
        public void EvaluateStrength_ShortOrSingleClass_IsWeak(string password)
        {
            Assert.Equal(PasswordStrength.Weak, PasswordHealthChecker.EvaluateStrength(password));
        }

        [Fact]
        public void EvaluateStrength_MediumLengthWithSomeVariety_IsMedium()
        {
            Assert.Equal(PasswordStrength.Medium, PasswordHealthChecker.EvaluateStrength("Abcdefgh1"));
        }

        [Fact]
        public void EvaluateStrength_LongWithFullVariety_IsStrong()
        {
            Assert.Equal(PasswordStrength.Strong, PasswordHealthChecker.EvaluateStrength("Tr7$kQmz!pLxN9wq"));
        }

        [Fact]
        public void EvaluateStrength_GeneratedPassword_IsStrong()
        {
            var pwd = PasswordGenerator.Generate(20, includeSymbols: true);

            Assert.Equal(PasswordStrength.Strong, PasswordHealthChecker.EvaluateStrength(pwd));
        }
    }
}
