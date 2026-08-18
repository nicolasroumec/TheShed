using System.ComponentModel.DataAnnotations;
using TheShed.Shared.Models.DTOs.Auth;
using Xunit;

namespace TheShed.Tests.Security
{
    /// <summary>
    /// The password length cap is what keeps an unauthenticated caller from making Argon2 —
    /// which is expensive on purpose — chew through an arbitrarily long input.
    /// </summary>
    public class AuthRequestValidationTests
    {
        private static bool IsValid(object request, out List<ValidationResult> errors)
        {
            errors = [];
            return Validator.TryValidateObject(request, new ValidationContext(request), errors, validateAllProperties: true);
        }

        [Theory]
        [InlineData(128, true)]
        [InlineData(129, false)]
        public void RegisterRequest_CapsPasswordAt128(int length, bool expectedValid)
        {
            var request = new RegisterRequest
            {
                Username = "shed-user",
                Email = "user@example.com",
                Password = new string('x', length),
                KeySalt = "c2FsdA=="
            };

            Assert.Equal(expectedValid, IsValid(request, out _));
        }

        [Theory]
        [InlineData(128, true)]
        [InlineData(129, false)]
        public void LoginRequest_CapsPasswordAt128(int length, bool expectedValid)
        {
            var request = new LoginRequest
            {
                Email = "user@example.com",
                Password = new string('x', length)
            };

            Assert.Equal(expectedValid, IsValid(request, out _));
        }

        [Fact]
        public void RegisterRequest_StillRejectsShortPasswords()
        {
            var request = new RegisterRequest
            {
                Username = "shed-user",
                Email = "user@example.com",
                Password = "short"
            };

            Assert.False(IsValid(request, out var errors));
            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RegisterRequest.Password)));
        }
    }
}
