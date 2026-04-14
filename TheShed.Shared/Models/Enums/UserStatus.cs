namespace TheShed.Shared.Models.Enums
{
    public enum UserStatus
    {
        PendingEmailVerification = 1,
        PendingAdminApproval = 2,
        Active = 3,
        Rejected = 4,
        Suspended = 5
    }
}
