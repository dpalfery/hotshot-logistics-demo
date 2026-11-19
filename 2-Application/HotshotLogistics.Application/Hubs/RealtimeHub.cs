using System.Security.Claims;
using HotshotLogistics.Application.Authorization;
using HotshotLogistics.Contracts.Hubs;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.Application.Hubs
{
    /// <summary>
    ///     SignalR hub for real-time communication with clients. Only authenticated callers may connect.
    /// </summary>
    [Authorize]
    public class RealtimeHub(
        ILogger<RealtimeHub> logger,
        ISignalRClientWrapper signalRClient,
        IConnectionManagerService connectionManager,
        IAuthorizationService authorization)
        : Hub<IRealtimeHubClient>
    {
        /// <summary>
        ///     Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            string userId = Context.UserIdentifier ?? Context.ConnectionId;
            await connectionManager.AddConnectionAsync(userId, Context.ConnectionId);
            logger.LogInformation("Client connected: User {UserId}, Connection {ConnectionId}", userId,
                Context.ConnectionId);

            await JoinAdminsGroupWhenAuthorizedAsync();

            await base.OnConnectedAsync();
        }

        /// <summary>
        ///     Adds the connection to the admin dashboard audience when the caller is a manager or admin.
        ///     Done on connect, so a reconnect (a new connection) is added again without any client call.
        /// </summary>
        private async Task JoinAdminsGroupWhenAuthorizedAsync()
        {
            ClaimsPrincipal? user = Context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            AuthorizationResult result =
                await authorization.AuthorizeAsync(user, null, AuthorizationPolicies.ManagerOrAdmin);
            if (!result.Succeeded)
            {
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Admins);
            logger.LogInformation("Connection {ConnectionId} joined the admin dashboard audience",
                Context.ConnectionId);
        }

        /// <summary>
        ///     Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string userId = Context.UserIdentifier ?? Context.ConnectionId;
            await connectionManager.RemoveConnectionAsync(Context.ConnectionId);
            logger.LogInformation("Client disconnected: User {UserId}, Connection {ConnectionId}", userId,
                Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        ///     Broadcast job status update to all connected clients tracking the job
        /// </summary>
        public async Task JobStatusUpdated(string jobId, JobStatus status)
        {
            try
            {
                await signalRClient.SendToGroupAsync(RealtimeGroups.Job(jobId), "JobStatusUpdated", jobId, status);
                logger.LogInformation("Job status update sent for job {JobId}: {Status}", jobId, status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting job status update for job {JobId}", jobId);
            }
        }

        /// <summary>
        ///     Broadcast location update to clients tracking the job
        /// </summary>
        public async Task LocationUpdated(string jobId, LocationUpdate location)
        {
            try
            {
                await signalRClient.SendToGroupAsync(RealtimeGroups.Job(jobId), "LocationUpdated", jobId, location);
                logger.LogDebug("Location update sent for job {JobId}", jobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting location update for job {JobId}", jobId);
            }
        }

        /// <summary>
        ///     Broadcast driver status change to relevant clients
        /// </summary>
        public async Task DriverStatusChanged(int driverId, DriverStatus status)
        {
            try
            {
                await signalRClient.SendToGroupAsync($"driver-{driverId}", "DriverStatusChanged", driverId, status);
                // Also notify admin clients
                await signalRClient.SendToGroupAsync(RealtimeGroups.Admins, "DriverStatusChanged", driverId, status);
                logger.LogInformation("Driver status update sent for driver {DriverId}: {Status}", driverId, status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting driver status update for driver {DriverId}", driverId);
            }
        }

        /// <summary>
        ///     Broadcast new job availability to available drivers
        /// </summary>
        public async Task NewJobAvailable(ContractsJobDto job)
        {
            try
            {
                await signalRClient.SendToGroupAsync("available-drivers", "NewJobAvailable", job);
                logger.LogInformation("New job availability broadcast for job {JobId}", job.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting new job availability for job {JobId}", job.Id);
            }
        }

        /// <summary>
        ///     Send notification to specific user or broadcast to all
        /// </summary>
        public async Task NotificationReceived(NotificationMessageDto message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message.UserId))
                {
                    await connectionManager.SendToUserAsync(message.UserId, "NotificationReceived", message);
                }
                else
                {
                    await signalRClient.SendToAllAsync("NotificationReceived", message);
                }

                logger.LogInformation("Notification sent: {Title}", message.Title);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending notification: {Title}", message.Title);
            }
        }

        /// <summary>
        ///     Join job tracking group
        /// </summary>
        public async Task JoinJobTracking(string jobId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Job(jobId));
                logger.LogInformation("Connection {ConnectionId} joined job tracking for {JobId}",
                    Context.ConnectionId, jobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error joining job tracking");
            }
        }

        /// <summary>
        ///     Leave job tracking group
        /// </summary>
        public async Task LeaveJobTracking(string jobId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Job(jobId));
                logger.LogInformation("Connection {ConnectionId} left job tracking for {JobId}",
                    Context.ConnectionId, jobId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error leaving job tracking");
            }
        }
    }
}
