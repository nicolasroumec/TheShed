namespace TheShed.Server.Enums
{
    /// <summary>Possible outcome of an authentication operation.</summary>
    public enum AuthError { None, EmailInUse, InvalidCredentials, VaultKeysOutOfDate }
}
