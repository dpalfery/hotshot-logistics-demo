using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;

namespace HotshotLogistics.Contracts.Services
{
    /// <summary>
    ///     Service interface for notification operations.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        ///     Sends an SMS notification.
        /// </summary>
        /// <param name="phoneNumber">The phone number to send to.</param>
        /// <param name="message">The message content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if SMS was sent successfully.</returns>
        Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Sends an email notification.
        /// </summary>
        /// <param name="emailAddress">The email address to send to.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="message">The email content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if email was sent successfully.</returns>
        Task<bool> SendEmailAsync(string emailAddress, string subject, string message,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Sends a push notification.
        /// </summary>
        /// <param name="deviceToken">The device token.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if push notification was sent successfully.</returns>
        Task<bool> SendPushNotificationAsync(string deviceToken, string title, string message,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Sends a notification to a user based on their preferences.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="notificationType">The type of notification.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if notification was sent successfully.</returns>
        Task<bool> SendNotificationAsync(string userId, NotificationType notificationType, string title, string message,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets notification preferences for a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user's notification preferences.</returns>
        Task<NotificationPreferences?> GetNotificationPreferencesAsync(string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Updates notification preferences for a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="preferences">The notification preferences.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if preferences were updated successfully.</returns>
        Task<bool> UpdateNotificationPreferencesAsync(string userId, NotificationPreferences preferences,
            CancellationToken cancellationToken = default);
    }
}
