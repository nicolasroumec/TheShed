namespace TheShed.Server.Services
{
    public class AttachmentSettings
    {
        public string StoragePath { get; set; } = "App_Data/attachments";
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB
    }
}
