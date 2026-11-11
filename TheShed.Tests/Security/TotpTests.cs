using System.Text;
using TheShed.Server.Security;
using Xunit;

namespace TheShed.Tests.Security
{
    /// <summary>Checked against the RFC 6238 Appendix B vectors (SHA-1 seed, last 6 of 8 digits).</summary>
    public class TotpTests
    {
        private static readonly byte[] RfcSecret = Encoding.ASCII.GetBytes("12345678901234567890");

        [Theory]
        [InlineData(59, "287082")]
        [InlineData(1111111109, "081804")]
        [InlineData(1234567890, "005924")]
        [InlineData(20000000000, "353130")]
        public void Code_MatchesRfcVectors(long unixTime, string expected) =>
            Assert.Equal(expected, Totp.Code(RfcSecret, Totp.StepAt(DateTimeOffset.FromUnixTimeSeconds(unixTime))));

        [Fact]
        public void MatchStep_AcceptsOneStepOfDriftEitherWay()
        {
            var now = DateTimeOffset.FromUnixTimeSeconds(1234567890);
            var step = Totp.StepAt(now);

            Assert.Equal(step - 1, Totp.MatchStep(RfcSecret, Totp.Code(RfcSecret, step - 1), now));
            Assert.Equal(step, Totp.MatchStep(RfcSecret, Totp.Code(RfcSecret, step), now));
            Assert.Equal(step + 1, Totp.MatchStep(RfcSecret, Totp.Code(RfcSecret, step + 1), now));
        }

        [Fact]
        public void MatchStep_RejectsCodesOutsideTheWindow()
        {
            var now = DateTimeOffset.FromUnixTimeSeconds(1234567890);
            var step = Totp.StepAt(now);

            Assert.Null(Totp.MatchStep(RfcSecret, Totp.Code(RfcSecret, step - 2), now));
            Assert.Null(Totp.MatchStep(RfcSecret, Totp.Code(RfcSecret, step + 2), now));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("12345")]
        [InlineData("1234567")]
        [InlineData("12a456")]
        [InlineData("１２３４５６")] // full-width digits pass char.IsDigit, not IsAsciiDigit
        public void MatchStep_RejectsMalformedCodes(string? code) =>
            Assert.Null(Totp.MatchStep(RfcSecret, code, DateTimeOffset.UtcNow));

        [Fact]
        public void ToBase32_MatchesRfc4648()
        {
            Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", Totp.ToBase32(RfcSecret));
            Assert.Equal("MZXW6YQ", Totp.ToBase32(Encoding.ASCII.GetBytes("foob"))); // partial final group
        }

        [Fact]
        public void GenerateSecret_Is160BitsAndRandom()
        {
            var a = Totp.GenerateSecret();
            Assert.Equal(20, a.Length);
            Assert.NotEqual(a, Totp.GenerateSecret());
        }
    }
}
