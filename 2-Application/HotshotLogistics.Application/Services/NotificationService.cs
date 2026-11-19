// <copyright file="NotificationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.Json;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Service for notification operations with multi-channel support.
    /// </summary>
    public class NotificationService : INotificationService
    {
        // Retry configuration
        private const int s_maxRetryAttempts = 3;
        private static readonly TimeSpan s_baseRetryDelay = TimeSpan.FromSeconds(1);
        private readonly IDistributedCache _cache;
        private readonly ICommunicationServiceFactory _communicationFactory;
        private readonly ILogger<NotificationService> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NotificationService" /> class.
        /// </summary>
        /// <param name="cache">The distributed _cache.</param>
        /// <param name="logger">The _logger.</param>
        /// <param name="communicationFactory">The communication service factory.</param>
        public NotificationService(
            IDistributedCache cache,
            ILogger<NotificationService> logger,
            ICommunicationServiceFactory communicationFactory)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _communicationFactory =
                communicationFactory ?? throw new ArgumentNullException(nameof(communicationFactory));
        }

        /// <inheritdoc />
        public async Task<bool> SendSmsAsync(string phoneNumber, string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(message));
            }

            return await ExecuteWithRetryAsync(async () =>
                {
                    _logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);

                    CommunicationMessage commMessage = new()
                    {
                        To = phoneNumber,
                        Body = message
                    };

                    ICommunicationService service = _communicationFactory.GetService("Sms");
                    return await service.SendAsync(commMessage, cancellationToken);
                }, $"SMS to {phoneNumber}");
        }

        /// <inheritdoc />
        public async Task<bool> SendEmailAsync(string emailAddress, string subject, string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                throw new ArgumentException("Email address cannot be empty", nameof(emailAddress));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException("Subject cannot be empty", nameof(subject));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(message));
            }

            return await ExecuteWithRetryAsync(async () =>
            {
                _logger.LogInformation("Sending email with subject: {Subject} to recipient", subject);

                CommunicationMessage commMessage = new()
                {
                    To = emailAddress,
                    Subject = subject,
                    Body = message
                };

                ICommunicationService service = _communicationFactory.GetService("Email");
                return await service.SendAsync(commMessage, cancellationToken);
            }, "Email notification");
        }

        /// <inheritdoc />
        public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                throw new ArgumentException("Device token cannot be empty", nameof(deviceToken));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be empty", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(message));
            }

            return await ExecuteWithRetryAsync(async () =>
                {
                    _logger.LogInformation("Sending push notification to device {DeviceToken}", deviceToken);

                    CommunicationMessage commMessage = new()
                    {
                        To = deviceToken,
                        Title = title,
                        Body = message
                    };

                    ICommunicationService service = _communicationFactory.GetService("Push");
                    return await service.SendAsync(commMessage, cancellationToken);
                }, $"Push notification to {deviceToken}");
        }

        /// <inheritdoc />
        public async Task<bool> SendNotificationAsync(string userId, NotificationType notificationType, string title,
            string message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending {NotificationType} notification to user {UserId}", notificationType,
                userId);

            NotificationPreferencesDto? preferences = await GetNotificationPreferencesAsync(userId, cancellationToken);
            if (preferences == null)
            {
                _logger.LogWarning("No notification preferences found for user {UserId}", userId);
                return false;
            }

            // Check if user wants to receive this type of notification
            if (!preferences.EnabledNotificationTypes.Contains(notificationType))
            {
                _logger.LogDebug("User {UserId} has disabled {NotificationType} notifications", userId,
                    notificationType);
                return true; // Return true as it's not an error, just user preference
            }

            List<bool> results = new();

            // Send via enabled channels
            if (preferences.SmsEnabled && !string.IsNullOrWhiteSpace(preferences.PhoneNumber))
            {
                bool smsResult = await SendSmsAsync(preferences.PhoneNumber, message, cancellationToken);
                results.Add(smsResult);
            }

            if (preferences.EmailEnabled && !string.IsNullOrWhiteSpace(preferences.EmailAddress))
            {
                bool emailResult = await SendEmailAsync(preferences.EmailAddress, title, message, cancellationToken);
                results.Add(emailResult);
            }

            if (preferences.PushEnabled && !string.IsNullOrWhiteSpace(preferences.DeviceToken))
            {
                bool pushResult =
                    await SendPushNotificationAsync(preferences.DeviceToken, title, message, cancellationToken);
                results.Add(pushResult);
            }

            // Return true if at least one notification was sent successfully
            bool success = results.Any() && results.Any(r => r);

            // Log notification to history
            await LogNotificationAsync(userId, notificationType, title, message, success, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Notification sent successfully to user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Failed to send notification to user {UserId}", userId);
            }

            return success;
        }

        /// <inheritdoc />
        public async Task<NotificationPreferencesDto?> GetNotificationPreferencesAsync(string userId,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"notification_preferences:{userId}";
            string? preferencesJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(preferencesJson))
            {
                try
                {
                    return JsonSerializer.Deserialize<NotificationPreferencesDto>(preferencesJson);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize notification preferences for user {UserId}", userId);
                }
            }

            // In real implementation, get from database
            // For demo purposes, return default preferences
            NotificationPreferencesDto defaultPreferences = new()
            {
                UserId = userId,
                SmsEnabled = true,
                EmailEnabled = true,
                PushEnabled = true,
                PhoneNumber = "+1234567890", // Demo phone number
                EmailAddress = $"user{userId}@example.com", // Demo email
                DeviceToken = $"device_token_{userId}", // Demo device token
                EnabledNotificationTypes = Enum.GetValues<NotificationType>().ToList()
            };

            // Cache the default preferences
            await UpdateNotificationPreferencesAsync(userId, defaultPreferences, cancellationToken);

            return defaultPreferences;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateNotificationPreferencesAsync(string userId, NotificationPreferencesDto preferences,
            CancellationToken cancellationToken = default)
        {
            if (preferences == null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            try
            {
                string cacheKey = $"notification_preferences:{userId}";
                string preferencesJson = JsonSerializer.Serialize(preferences);

                await _cache.SetStringAsync(cacheKey, preferencesJson, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromDays(30) // Cache for 30 days
                }, cancellationToken);

                _logger.LogInformation("Notification preferences updated for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update notification preferences for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        ///     Gets notification history for a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="startDate">The start date for the history.</param>
        /// <param name="endDate">The end date for the history.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The notification history.</returns>
        public async Task<List<NotificationHistory>> GetNotificationHistoryAsync(string userId,
            DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting notification history for user {UserId}", userId);

            string cacheKey = $"notification_history:{userId}";
            string? historyJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

            List<NotificationHistory> history = new();

            if (!string.IsNullOrEmpty(historyJson))
            {
                try
                {
                    history = JsonSerializer.Deserialize<List<NotificationHistory>>(historyJson) ??
                              new List<NotificationHistory>();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize notification history for user {UserId}", userId);
                }
            }

            // Filter by date range if specified
            if (startDate.HasValue || endDate.HasValue)
            {
                history = history.Where(h =>
                    (!startDate.HasValue || h.SentAt >= startDate.Value) &&
                    (!endDate.HasValue || h.SentAt <= endDate.Value)
                ).ToList();
            }

            _logger.LogDebug("Retrieved {Count} notification history records for user {UserId}", history.Count, userId);
            return history;
        }

        /// <summary>
        ///     Sends batch notifications to multiple users.
        /// </summary>
        /// <param name="userIds">The list of user identifiers.</param>
        /// <param name="notificationType">The type of notification.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of notifications sent successfully.</returns>
        public async Task<int> SendBatchNotificationAsync(List<string> userIds, NotificationType notificationType,
            string title, string message, CancellationToken cancellationToken = default)
        {
            if (userIds == null || !userIds.Any())
            {
                throw new ArgumentException("User IDs list cannot be empty", nameof(userIds));
            }

            _logger.LogInformation("Sending batch {NotificationType} notification to {UserCount} users",
                notificationType, userIds.Count);

            int successCount = 0;

            // Throttle batch sending to prevent overwhelming the system
            const int batchSize = 10;
            for (int i = 0; i < userIds.Count; i += batchSize)
            {
                List<string> batch = userIds.Skip(i).Take(batchSize).ToList();
                List<Task<bool>> batchTasks = batch.Select(userId =>
                    SendNotificationAsync(userId, notificationType, title, message, cancellationToken)).ToList();

                // Wait for current batch to complete before starting next batch
                bool[] batchResults = await Task.WhenAll(batchTasks);
                successCount += batchResults.Count(r => r);

                // Small delay between batches to prevent rate limiting
                if (i + batchSize < userIds.Count)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            _logger.LogInformation("Batch notification completed: {SuccessCount}/{TotalCount} sent successfully",
                successCount, userIds.Count);
            return successCount;
        }

        /// <summary>
        ///     Sends an urgent notification using all available channels simultaneously.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if at least one channel succeeded.</returns>
        public async Task<bool> SendUrgentNotificationAsync(string userId, string title, string message,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending urgent notification to user {UserId}", userId);

            NotificationPreferencesDto? preferences = await GetNotificationPreferencesAsync(userId, cancellationToken);
            if (preferences == null)
            {
                _logger.LogWarning("No notification preferences found for user {UserId}", userId);
                return false;
            }

            List<Task<bool>> tasks = new();

            // Send via all available channels simultaneously for urgent notifications
            if (!string.IsNullOrWhiteSpace(preferences.PhoneNumber))
            {
                tasks.Add(SendSmsAsync(preferences.PhoneNumber, $"URGENT: {message}", cancellationToken));
            }

            if (!string.IsNullOrWhiteSpace(preferences.EmailAddress))
            {
                tasks.Add(SendEmailAsync(preferences.EmailAddress, $"URGENT: {title}", message, cancellationToken));
            }

            if (!string.IsNullOrWhiteSpace(preferences.DeviceToken))
            {
                tasks.Add(SendPushNotificationAsync(preferences.DeviceToken, $"URGENT: {title}", message,
                    cancellationToken));
            }

            if (tasks.Count == 0)
            {
                _logger.LogWarning("No notification channels available for user {UserId}", userId);
                return false;
            }

            bool[] results = await Task.WhenAll(tasks);
            bool success = results.Any(r => r);

            // Log notification to history
            await LogNotificationAsync(userId, NotificationType.Emergency, title, message, success, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Urgent notification sent successfully to user {UserId}", userId);
            }
            else
            {
                _logger.LogError("Failed to send urgent notification to user {UserId} via all channels", userId);
            }

            return success;
        }

        /// <summary>
        ///     Validates notification preferences.
        /// </summary>
        /// <param name="preferences">The notification preferences to validate.</param>
        /// <returns>True if preferences are valid, false otherwise.</returns>
        public bool ValidateNotificationPreferences(NotificationPreferencesDto? preferences)
        {
            if (preferences == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(preferences.UserId))
            {
                return false;
            }

            // Validate phone number format if SMS is enabled
            if (preferences.SmsEnabled && !string.IsNullOrWhiteSpace(preferences.PhoneNumber))
            {
                if (!IsValidPhoneNumber(preferences.PhoneNumber))
                {
                    _logger.LogWarning("Invalid phone number format: {PhoneNumber}", preferences.PhoneNumber);
                    return false;
                }
            }

            // Validate email format if email is enabled
            if (preferences.EmailEnabled && !string.IsNullOrWhiteSpace(preferences.EmailAddress))
            {
                if (!IsValidEmail(preferences.EmailAddress))
                {
                    _logger.LogWarning("Invalid email format: {EmailAddress}", preferences.EmailAddress);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Logs a notification to the user's history.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="notificationType">The notification type.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="success">Whether the notification was sent successfully.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task LogNotificationAsync(string userId, NotificationType notificationType, string title,
            string message, bool success, CancellationToken cancellationToken)
        {
            try
            {
                List<NotificationHistory> history =
                    await GetNotificationHistoryAsync(userId, cancellationToken: cancellationToken);

                NotificationHistory notification = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Type = notificationType,
                    Title = title,
                    Message = message,
                    SentAt = DateTime.UtcNow,
                    Success = success
                };

                history.Add(notification);

                // Keep only last 100 notifications per user
                if (history.Count > 100)
                {
                    history = history.OrderByDescending(h => h.SentAt).Take(100).ToList();
                }

                string cacheKey = $"notification_history:{userId}";
                string historyJson = JsonSerializer.Serialize(history);

                await _cache.SetStringAsync(cacheKey, historyJson, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromDays(30)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log notification history for user {UserId}", userId);
            }
        }

        /// <summary>
        ///     Validates a phone number format.
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            // Simple validation - in real implementation, use a proper phone number validation library
            return !string.IsNullOrWhiteSpace(phoneNumber) &&
                   phoneNumber.Length >= 10 &&
                   phoneNumber.All(c => char.IsDigit(c) || c == '+' || c == '-' || c == '(' || c == ')' || c == ' ');
        }

        /// <summary>
        ///     Validates an email address format.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool IsValidEmail(string email)
        {
            // Simple validation - in real implementation, use a proper email validation library
            return !string.IsNullOrWhiteSpace(email) &&
                   email.Contains('@') &&
                   email.Contains('.') &&
                   email.Length > 5;
        }

        /// <summary>
        ///     Executes an operation with exponential backoff retry logic.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for logging.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        private async Task<bool> ExecuteWithRetryAsync(Func<Task<bool>> operation, string operationName)
        {
            for (int attempt = 1; attempt <= s_maxRetryAttempts; attempt++)
            {
                try
                {
                    bool result = await operation();
                    if (result)
                    {
                        return true;
                    }

                    if (attempt < s_maxRetryAttempts)
                    {
                        TimeSpan delay =
                            TimeSpan.FromMilliseconds(s_baseRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                        _logger.LogInformation(
                            "Retrying {OperationName} in {Delay}ms (attempt {Attempt}/{MaxAttempts})",
                            operationName, delay.TotalMilliseconds, attempt, s_maxRetryAttempts);
                        await Task.Delay(delay);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed for {OperationName}",
                        attempt, s_maxRetryAttempts, operationName);

                    if (attempt == s_maxRetryAttempts)
                    {
                        _logger.LogError(ex, "All retry attempts failed for {OperationName}", operationName);
                        return false;
                    }

                    TimeSpan delay =
                        TimeSpan.FromMilliseconds(s_baseRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                }
            }

            return false;
        }
    }
}
