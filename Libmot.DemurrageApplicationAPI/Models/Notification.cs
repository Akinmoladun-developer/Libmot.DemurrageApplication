namespace Libmot.DemurrageApplicationAPI.Models
{
    public enum NotificationType { Email, SMS }
    public enum NotificationStatus { Pending, Sent, Failed }

    public class Notification
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }
        public string Recipient { get; set; } = string.Empty;  // email or phone
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
    }
}
