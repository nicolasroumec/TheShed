namespace TheShed.Server.Security
{
    /// <summary>
    /// Configuración del cifrado de entradas. La clave se provee por
    /// User Secrets (dev) o variables de entorno (prod), nunca en appsettings.json.
    /// </summary>
    public class EncryptionSettings
    {
        /// <summary>Clave AES-256 en base64 (debe decodificar a exactamente 32 bytes).</summary>
        public string Key { get; set; } = string.Empty;
    }
}
