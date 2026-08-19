using System.Security.Cryptography;

namespace TheShed.Shared.Helpers
{
    /// <summary>Generates strong random passwords using a cryptographically secure RNG.
    /// Always includes a lowercase letter, an uppercase letter and a digit; symbols are
    /// optional. Each required character class is guaranteed to appear at least once.</summary>
    public static class PasswordGenerator
    {
        private const string Lower = "abcdefghijklmnopqrstuvwxyz";
        private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digits = "0123456789";
        private const string Symbols = "!@#$%^&*()-_=+[]{};:,.?";

        public static string Generate(int length = 20, bool includeSymbols = true)
        {
            var classes = includeSymbols
                ? new[] { Lower, Upper, Digits, Symbols }
                : new[] { Lower, Upper, Digits };

            if (length < classes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length),
                    $"Length must be at least {classes.Length} to include every character class.");
            }

            var all = string.Concat(classes);
            var chars = new char[length];

            // One guaranteed character from each required class...
            for (var i = 0; i < classes.Length; i++)
            {
                chars[i] = Pick(classes[i]);
            }
            // ...then fill the rest from the combined pool.
            for (var i = classes.Length; i < length; i++)
            {
                chars[i] = Pick(all);
            }

            // Shuffle so the guaranteed characters are not stuck at the front.
            RandomNumberGenerator.Shuffle(chars.AsSpan());
            return new string(chars);
        }

        private static char Pick(string pool) => pool[RandomNumberGenerator.GetInt32(pool.Length)];

        /// <summary>Generates a memorable passphrase from a curated word list
        /// (<see cref="PassphraseWordList"/>), e.g. "correct-horse-battery-staple".</summary>
        public static string GeneratePassphrase(int wordCount = 4, string separator = "-",
            bool capitalizeFirst = false, bool includeNumber = false)
        {
            if (wordCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(wordCount), "Word count must be at least 1.");
            }

            var pool = PassphraseWordList.Words;
            var words = new string[wordCount];
            for (var i = 0; i < wordCount; i++)
            {
                var word = pool[RandomNumberGenerator.GetInt32(pool.Length)];
                words[i] = capitalizeFirst ? char.ToUpperInvariant(word[0]) + word[1..] : word;
            }

            var passphrase = string.Join(separator, words);
            return includeNumber
                ? passphrase + separator + RandomNumberGenerator.GetInt32(10)
                : passphrase;
        }
    }
}
