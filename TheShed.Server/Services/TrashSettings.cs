namespace TheShed.Server.Services
{
    /// <summary>How long a soft-deleted item stays in the trash before the automatic purge removes it.</summary>
    public class TrashSettings
    {
        public int RetentionDays { get; set; } = 30;
    }
}
