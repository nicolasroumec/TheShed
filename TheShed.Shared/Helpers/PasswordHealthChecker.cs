namespace TheShed.Shared.Helpers
{
    public enum PasswordStrength
    {
        Weak,
        Medium,
        Strong
    }

    /// <summary>Rates password strength from length, character variety and a simple
    /// entropy estimate (length * log2(pool size)). Heuristic, not a full zxcvbn-style
    /// dictionary/pattern check.</summary>
    public static class PasswordHealthChecker
    {
        public static PasswordStrength EvaluateStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return PasswordStrength.Weak;
            }

            var poolSize = 0;
            var varietyClasses = 0;

            if (password.Any(char.IsLower)) { poolSize += 26; varietyClasses++; }
            if (password.Any(char.IsUpper)) { poolSize += 26; varietyClasses++; }
            if (password.Any(char.IsDigit)) { poolSize += 10; varietyClasses++; }
            if (password.Any(c => !char.IsLetterOrDigit(c))) { poolSize += 32; varietyClasses++; }

            var entropyBits = password.Length * Math.Log2(Math.Max(poolSize, 1));

            if (password.Length < 8 || varietyClasses <= 1 || entropyBits < 40)
            {
                return PasswordStrength.Weak;
            }
            if (password.Length < 12 || varietyClasses < 3 || entropyBits < 60)
            {
                return PasswordStrength.Medium;
            }
            return PasswordStrength.Strong;
        }
    }
}
