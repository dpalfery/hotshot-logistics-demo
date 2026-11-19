using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Represents notification preferences for a user.
    /// </summary>
    public class NotificationPreferences
    {
        /// <summary>
        ///     Gets or sets the user identifier.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets a value indicating whether SMS notifications are enabled.
        /// </summary>
        public bool SmsEnabled { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether email notifications are enabled.
        /// </summary>
        public bool EmailEnabled { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether push notifications are enabled.
        /// </summary>
        public bool PushEnabled { get; set; }

        /// <summary>
        ///     Gets or sets the phone number for SMS notifications.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        ///     Gets or sets the email address for email notifications.
        /// </summary>
        public string? EmailAddress { get; set; }

        /// <summary>
        ///     Gets or sets the device token for push notifications.
        /// </summary>
        public string? DeviceToken { get; set; }

        /// <summary>
        ///     Gets or sets the notification types the user wants to receive.
        /// </summary>
        public List<NotificationType> EnabledNotificationTypes { get; set; } = new();
    }
}
