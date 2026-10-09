namespace TheShed.Server.Security
{
    /// <summary>
    /// Access JWT settings. The signing key (Key) comes from User Secrets (dev) or
    /// environment variables (prod), never from appsettings.json.
    /// </summary>
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        /// <summary>HMAC-SHA256 signing key (32 bytes minimum).</summary>
        public string Key { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;
    }
}
