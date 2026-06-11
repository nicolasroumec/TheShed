namespace TheShed.Server.Security
{
    /// <summary>
    /// Configuración del JWT de acceso. La clave (Key) se provee por
    /// User Secrets (dev) o variables de entorno (prod), nunca en appsettings.json.
    /// </summary>
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        /// <summary>Clave de firma HMAC-SHA256 (mínimo 32 bytes).</summary>
        public string Key { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;
    }
}
