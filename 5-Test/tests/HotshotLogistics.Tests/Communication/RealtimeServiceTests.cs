using FluentAssertions;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Contracts.Hubs;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Communication
{
    /// <summary>
    ///     Unit tests for RealtimeService.
    /// </summary>
    public class RealtimeServiceTests
    {
        private readonly Mock<ILogger<RealtimeService>> _mockLogger;
        private readonly Mock<ISignalRClientWrapper> _mockSignalRClient;
        private readonly RealtimeService _realtimeService;

        public RealtimeServiceTests()
        {
            _mockSignalRClient = new Mock<ISignalRClientWrapper>();
            _mockLogger = new Mock<ILogger<RealtimeService>>();

            _realtimeService = new RealtimeService(_mockSignalRClient.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task BroadcastJobStatusUpdate_ShouldSendToJobGroup()
        {
            // Arrange
            string jobId = "job-123";
            JobStatus status = JobStatus.EnRoute;

            // Act
            await _realtimeService.BroadcastJobStatusUpdate(jobId, status);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToGroupAsync($"job-{jobId}", "JobStatusUpdated", jobId, status),
                Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"Job status update broadcast for job {jobId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastLocationUpdate_ShouldSendToJobGroup()
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
            await _realtimeService.BroadcastLocationUpdate(jobId, location);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToGroupAsync($"job-{jobId}", "LocationUpdated", jobId, location),
                Times.Once);

            // Verify debug logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains($"Location update broadcast for job {jobId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastDriverStatusChange_ShouldSendToDriverGroupAndAdmins()
        {
            // Arrange
            int driverId = 123;
            DriverStatus status = DriverStatus.Available;

            // Act
            await _realtimeService.BroadcastDriverStatusChange(driverId, status);

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
                        v!.ToString()!.Contains($"Driver status update broadcast for driver {driverId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastNewJobAvailable_ShouldSendToAvailableDrivers()
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
            await _realtimeService.BroadcastNewJobAvailable(job);

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
        public async Task SendNotification_WithUserId_ShouldSendToSpecificUser()
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
            await _realtimeService.SendNotification(message);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToUserAsync(message.UserId, "NotificationReceived", message),
                Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"Notification sent to user {message.UserId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendNotification_WithoutUserId_ShouldBroadcastToAll()
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
            await _realtimeService.SendNotification(message);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToAllAsync("NotificationReceived", message), Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Notification broadcast to all users")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendToGroup_ShouldSendMessageToGroup()
        {
            // Arrange
            string groupName = "test-group";
            string method = "TestMethod";
            var data = new { message = "test data" };

            // Act
            await _realtimeService.SendToGroup(groupName, method, data);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToGroupAsync(groupName, method, data), Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"Message sent to group {groupName} via method {method}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendToUser_ShouldSendMessageToUser()
        {
            // Arrange
            string userId = "user-123";
            string method = "TestMethod";
            var data = new { message = "test data" };

            // Act
            await _realtimeService.SendToUser(userId, method, data);

            // Assert
            _mockSignalRClient.Verify(x => x.SendToUserAsync(userId, method, data), Times.Once);

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"Message sent to user {userId} via method {method}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BroadcastJobStatusUpdate_WhenExceptionThrown_ShouldLogErrorAndRethrow()
        {
            // Arrange
            string jobId = "job-123";
            JobStatus status = JobStatus.EnRoute;
            Exception expectedException = new("SignalR error");

            _mockSignalRClient.Setup(x => x.SendToGroupAsync($"job-{jobId}", "JobStatusUpdated", jobId, status))
                .ThrowsAsync(expectedException);

            // Act & Assert
            Exception exception = await Assert.ThrowsAsync<Exception>(() =>
                _realtimeService.BroadcastJobStatusUpdate(jobId, status));

            exception.Should().Be(expectedException);

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
        public async Task AddUserToGroup_ShouldLogIntent()
        {
            // Arrange
            string userId = "user-123";
            string groupName = "test-group";

            // Act
            await _realtimeService.AddUserToGroup(userId, groupName);

            // Assert - Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"Request to add user {userId} to group {groupName}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RemoveUserFromGroup_ShouldLogIntent()
        {
            // Arrange
            string userId = "user-123";
            string groupName = "test-group";

            // Act
            await _realtimeService.RemoveUserFromGroup(userId, groupName);

            // Assert - Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v!.ToString()!.Contains($"Request to remove user {userId} from group {groupName}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
