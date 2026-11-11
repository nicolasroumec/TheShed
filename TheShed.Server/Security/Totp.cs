using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace TheShed.Server.Security
{
    /// <summary>
    /// RFC 6238 TOTP (HMAC-SHA1, 6 digits, 30-second steps) — the defaults every authenticator app
    /// assumes. SHA-1 is fine here: HOTP relies on HMAC's PRF property, not collision resistance,
    /// and it is never used for password hashing.
    /// </summary>
    public static class Totp
    {
        public const int StepSeconds = 30;
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>160-bit secret, the size RFC 4226 recommends.</summary>
        public static byte[] GenerateSecret() => RandomNumberGenerator.GetBytes(20);

        public static long StepAt(DateTimeOffset time) => time.ToUnixTimeSeconds() / StepSeconds;

        public static string Code(byte[] secret, long step)
        {
            var counter = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(counter, step);
            var hash = HMACSHA1.HashData(secret, counter);
            var offset = hash[^1] & 0x0F;
            var binary = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset)) & 0x7FFFFFFF;
            return (binary % 1_000_000).ToString("D6");
        }

        /// <summary>
        /// Returns the step the code matches within ±<paramref name="window"/> steps of
        /// <paramref name="now"/> (clock drift), or null. Callers store the returned step and
        /// reject anything at or below it, so a code can't be replayed (RFC 6238 §5.2).
        /// </summary>
        public static long? MatchStep(byte[] secret, string? code, DateTimeOffset now, int window = 1)
        {
            if (code is null || code.Length != 6 || !code.All(char.IsAsciiDigit))
            {
                return null;
            }

            var given = Encoding.ASCII.GetBytes(code);
            var current = StepAt(now);
            for (var step = current - window; step <= current + window; step++)
            {
                if (CryptographicOperations.FixedTimeEquals(given, Encoding.ASCII.GetBytes(Code(secret, step))))
                {
                    return step;
                }
            }
            return null;
        }

        /// <summary>RFC 4648 base32 without padding — the format authenticator apps expect for the
        /// secret (in the otpauth:// URI and for manual entry).</summary>
        public static string ToBase32(byte[] data)
        {
            var sb = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = 0, bits = 0;
            foreach (var b in data)
            {
                buffer = (buffer << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    sb.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            if (bits > 0)
            {
                sb.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
            }
            return sb.ToString();
        }
    }
}
