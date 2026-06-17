using System.Security.Claims;
using System.Text.Json;

namespace TheShed.Client.Auth
{
    public static class JwtParser
    {
        public static IEnumerable<Claim> ParseClaims(string jwt)
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
            {
                return Enumerable.Empty<Claim>();
            }

            var payload = parts[1];
            var bytes = Base64UrlDecode(payload);
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bytes);
            if (values is null)
            {
                return Enumerable.Empty<Claim>();
            }

            return values.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));
        }

        public static DateTime? GetExpiry(string jwt)
        {
            var exp = ParseClaims(jwt).FirstOrDefault(c => c.Type == "exp");
            if (exp is not null && long.TryParse(exp.Value, out var seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }
            return null;
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input.Replace('-', '+').Replace('_', '/');
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            return Convert.FromBase64String(output);
        }
    }
}
