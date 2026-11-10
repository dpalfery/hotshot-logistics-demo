using HotshotLogistics.Application.Hubs;
using HotshotLogistics.Contracts.Hubs;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Communication
{
    /// <summary>
    ///     Unit tests for RealtimeHub.
    /// </summary>
    public class RealtimeHubTests
    {
        private readonly Mock<IConnectionManagerService> _mockConnectionManager;
        private readonly Mock<ILogger<RealtimeHub>> _mockLogger;
        private readonly Mock<ISignalRClientWrapper> _mockSignalRClient;
        private readonly RealtimeHub _realtimeHub;

        public RealtimeHubTests()
        {
            _mockLogger = new Mock<ILogger<RealtimeHub>>();
            _mockSignalRClient = new Mock<ISignalRClientWrapper>();
            _mockConnectionManager = new Mock<IConnectionManagerService>();

            _realtimeHub =
                new RealtimeHub(_mockLogger.Object, _mockSignalRClient.Object, _mockConnectionManager.Object);
        }

        [Fact]
        public async Task JobStatusUpdated_ShouldBroadcastToJobGroup()
        {
            // Arrange
            string jobId = "job-123";
            JobStatus status = JobStatus.EnRoute;

            // Act
            await _realtimeHub.JobStatusUpdated(jobId, status);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToGroupAsync($"job-{jobId}", "JobStatusUpdated", jobId, status),
                Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains($"Job status update sent for job {jobId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LocationUpdated_ShouldBroadcastToJobGroup()
        {
            // Arrange
            string jobId = "job-123";
            LocationUpdate location = new()
            {
                Latitude = 40.7128m,
                Longitude = -74.0060m,
                Timestamp = DateTime.UtcNow,
                Speed = 65.5m,
                Heading = 180,
                Accuracy = 5.0m
            };

            // Act
            await _realtimeHub.LocationUpdated(jobId, location);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToGroupAsync($"job-{jobId}", "LocationUpdated", jobId, location),
                Times.Once);
        }

        [Fact]
        public async Task DriverStatusChanged_ShouldBroadcastToDriverGroupAndAdmins()
        {
            // Arrange
            int driverId = 123;
            DriverStatus status = DriverStatus.Available;

            // Act
            await _realtimeHub.DriverStatusChanged(driverId, status);

            // Assert
            _mockSignalRClient.Verify(
                x => x.SendToGroupAsync($"driver-{driverId}", "DriverStatusChanged", driverId, status), Times.Once);
            _mockSignalRClient.Verify(x => x.SendToGroupAsync("admins", "DriverStatusChanged", driverId, status),
                Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"Driver status update sent for driver {driverId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task NewJobAvailable_ShouldBroadcastToAvailableDrivers()
        {
            // Arrange
            ContractsJobDto job = new()
            {
                Id = "job-123",
                CustomerId = "customer-456",
                PickupAddress = "123 Main St",
                DropoffAddress = "456 Oak Ave",
                Status = JobStatus.Pending
            };

            // Act
            await _realtimeHub.NewJobAvailable(job);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToGroupAsync("available-drivers", "NewJobAvailable", job), Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"New job availability broadcast for job {job.Id}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task NotificationReceived_WithUserId_ShouldSendToSpecificUser()
        {
            // Arrange
            NotificationMessageDto message = new()
            {
                Id = "notif-123",
                Title = "Test Notification",
                Message = "This is a test message",
                Type = NotificationType.Information,
                Timestamp = DateTime.UtcNow,
                UserId = "user-456"
            };

            // Act
            await _realtimeHub.NotificationReceived(message);

            // Assert
            _mockConnectionManager.Verify(x => x.SendToUserAsync(message.UserId, "NotificationReceived", message),
                Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains($"Notification sent: {message.Title}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task NotificationReceived_WithoutUserId_ShouldBroadcastToAll()
        {
            // Arrange
            NotificationMessageDto message = new()
            {
                Id = "notif-123",
                Title = "System Alert",
                Message = "This is a system-wide message",
                Type = NotificationType.SystemAlert,
                Timestamp = DateTime.UtcNow,
                UserId = null
            };

            // Act
            await _realtimeHub.NotificationReceived(message);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToAllAsync("NotificationReceived", message), Times.Once);
        }

        [Fact]
        public async Task JobStatusUpdated_WhenExceptionThrown_ShouldLogError()
        {
            // Arrange
            string jobId = "job-123";
            JobStatus status = JobStatus.EnRoute;
            Exception expectedException = new("SignalR error");

            _mockSignalRClient.Setup(x => x.SendToGroupAsync($"job-{jobId}", "JobStatusUpdated", jobId, status))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await _realtimeHub.JobStatusUpdated(jobId, status);

            // Verify error logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"Error broadcasting job status update for job {jobId}")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LocationUpdated_WhenExceptionThrown_ShouldLogError()
        {
            // Arrange
            string jobId = "job-123";
            LocationUpdate location = new()
            {
                Latitude = 40.7128m,
                Longitude = -74.0060m,
                Timestamp = DateTime.UtcNow
            };
            Exception expectedException = new("SignalR error");

            _mockSignalRClient.Setup(x => x.SendToGroupAsync($"job-{jobId}", "LocationUpdated", jobId, location))
                .ThrowsAsync(expectedException);

            // Act
            await _realtimeHub.LocationUpdated(jobId, location);

            // Assert - Verify error logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"Error broadcasting location update for job {jobId}")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(JobStatus.Pending)]
        [InlineData(JobStatus.Assigned)]
        [InlineData(JobStatus.EnRoute)]
        [InlineData(JobStatus.Received)]
        public async Task JobStatusUpdated_WithDifferentStatuses_ShouldBroadcastCorrectly(JobStatus status)
        {
            // Arrange
            string jobId = "job-123";

            // Act
            await _realtimeHub.JobStatusUpdated(jobId, status);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToGroupAsync($"job-{jobId}", "JobStatusUpdated", jobId, status),
                Times.Once);
        }

        [Theory]
        [InlineData(DriverStatus.Available)]
        [InlineData(DriverStatus.Delivering)]
        [InlineData(DriverStatus.Offline)]
        public async Task DriverStatusChanged_WithDifferentStatuses_ShouldBroadcastCorrectly(DriverStatus status)
        {
            // Arrange
            int driverId = 123;

            // Act
            await _realtimeHub.DriverStatusChanged(driverId, status);

            // Assert
            _mockSignalRClient.Verify(
                x => x.SendToGroupAsync($"driver-{driverId}", "DriverStatusChanged", driverId, status), Times.Once);
            _mockSignalRClient.Verify(x => x.SendToGroupAsync("admins", "DriverStatusChanged", driverId, status),
                Times.Once);
        }
    }
}
