using HotshotLogistics.Application.Hubs;
using HotshotLogistics.Contracts.Hubs;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Service for managing real-time communications via SignalR
    /// </summary>
    public class RealtimeService(ISignalRClientWrapper signalRClient, ILogger<RealtimeService> logger)
        : IRealtimeService
    {
        /// <summary>
        ///     Broadcast location update to clients tracking the job
        /// </summary>
        public async Task BroadcastLocationUpdate(string jobId, LocationUpdate location)
        {
            try
            {
                await signalRClient.SendToGroupAsync($"job-{jobId}", "LocationUpdated", jobId, location);

                logger.LogDebug("Location update broadcast for job {JobId}", jobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting location update for job {JobId}", jobId);
                throw;
            }
        }

        /// <summary>
        ///     Add user to a specific group for targeted messaging
        /// </summary>
        public Task AddUserToGroup(string userId, string groupName)
        {
            try
            {
                // Note: In Azure SignalR Service, we need the connection ID to add to groups
                // This method would typically be called when we have the connection context
                // For now, we'll log the intent and handle this in the hub methods
                logger.LogInformation("Request to add user {UserId} to group {GroupName}", userId, groupName);

                // In a real implementation, you'd need to track user connections
                // and add all their connections to the group
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding user {UserId} to group {GroupName}", userId, groupName);
                throw;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Remove user from a specific group
        /// </summary>
        public Task RemoveUserFromGroup(string userId, string groupName)
        {
            try
            {
                logger.LogInformation("Request to remove user {UserId} from group {GroupName}", userId, groupName);

                // In a real implementation, you'd need to track user connections
                // and remove all their connections from the group
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing user {UserId} from group {GroupName}", userId, groupName);
                throw;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Send message to all users in a specific group
        /// </summary>
        public async Task SendToGroup(string groupName, string method, object data)
        {
            try
            {
                await signalRClient.SendToGroupAsync(groupName, method, data);

                logger.LogInformation("Message sent to group {GroupName} via method {Method}", groupName, method);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message to group {GroupName}", groupName);
                throw;
            }
        }

        /// <summary>
        ///     Send message to a specific user
        /// </summary>
        public async Task SendToUser(string userId, string method, object data)
        {
            try
            {
                await signalRClient.SendToUserAsync(userId, method, data);

                logger.LogInformation("Message sent to user {UserId} via method {Method}", userId, method);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending message to user {UserId}", userId);
                throw;
            }
        }

        Task IRealtimeService.BroadcastJobStatusUpdate(string jobId, JobStatus status)
        {
            return BroadcastJobStatusUpdate(jobId, status);
        }

        Task IRealtimeService.BroadcastDriverStatusChange(int driverId, DriverStatus status)
        {
            return BroadcastDriverStatusChange(driverId, status);
        }

        Task IRealtimeService.BroadcastNewJobAvailable(ContractsJobDto job)
        {
            return BroadcastNewJobAvailable(job);
        }

        Task IRealtimeService.SendNotification(NotificationMessageDto message)
        {
            return SendNotification(message);
        }

        /// <summary>
        ///     Broadcast job status update to the admin dashboard and to the clients tracking the job
        /// </summary>
        public async Task BroadcastJobStatusUpdate(string jobId, JobStatus status)
        {
            try
            {
                await signalRClient.SendToGroupAsync(RealtimeGroups.Admins, "JobStatusUpdated", jobId, status);
                await signalRClient.SendToGroupAsync(RealtimeGroups.Job(jobId), "JobStatusUpdated", jobId, status);

                logger.LogInformation("Job status update broadcast for job {JobId}: {Status}", jobId, status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting job status update for job {JobId}", jobId);
                throw;
            }
        }

        /// <summary>
        ///     Broadcast driver status change to relevant clients
        /// </summary>
        public async Task BroadcastDriverStatusChange(int driverId, DriverStatus status)
        {
            try
            {
                // Notify clients tracking this specific driver
                await signalRClient.SendToGroupAsync($"driver-{driverId}", "DriverStatusChanged", driverId, status);

                // Also notify admin clients
                await signalRClient.SendToGroupAsync(RealtimeGroups.Admins, "DriverStatusChanged", driverId, status);

                logger.LogInformation("Driver status update broadcast for driver {DriverId}: {Status}", driverId,
                    status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting driver status update for driver {DriverId}", driverId);
                throw;
            }
        }

        /// <summary>
        ///     Broadcast new job availability to available drivers
        /// </summary>
        public async Task BroadcastNewJobAvailable(ContractsJobDto job)
        {
            try
            {
                await signalRClient.SendToGroupAsync("available-drivers", "NewJobAvailable", job);

                logger.LogInformation("New job availability broadcast for job {JobId}", job.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting new job availability for job {JobId}", job.Id);
                throw;
            }
        }

        /// <summary>
        ///     Send notification to specific user or broadcast to all
        /// </summary>
        public async Task SendNotification(NotificationMessageDto message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message.UserId))
                {
                    await signalRClient.SendToUserAsync(message.UserId, "NotificationReceived", message);

                    logger.LogInformation("Notification sent to user {UserId}: {Title}", message.UserId,
                        message.Title);
                }
                else
                {
                    await signalRClient.SendToAllAsync("NotificationReceived", message);

                    logger.LogInformation("Notification broadcast to all users: {Title}", message.Title);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending notification: {Title}", message.Title);
                throw;
            }
        }
    }
}
