namespace TheShed.Server.Enums
{
    /// <summary>Outcome of a password-entry or vault operation.</summary>
    // ponytail: reused as the generic ServiceError; UserNotFound/AlreadyMember are
    // vault-sharing outcomes. Rename the enum to ServiceError when it next grows.
    public enum EntryError { None, NotFound, Forbidden, UserNotFound, AlreadyMember }
}
