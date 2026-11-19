using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    /// <summary>
    ///     Unit tests for domain and application services.
    /// </summary>
    public class DriverServiceTests
    {
        [Fact]
        public async Task GetDriversAsync_ReturnsDriversFromRepository()
        {
            List<Driver> expected = [new() { Id = 1 }, new() { Id = 2 }];
            Mock<IDriverRepository> repoMock = new();
            repoMock.Setup(r => r.GetDriversAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
            DriverService service = new(repoMock.Object);
            IEnumerable<Driver> result = await service.GetDriversAsync();
            Assert.Equal(expected, result);
            repoMock.Verify(r => r.GetDriversAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetDriverByIdAsync_UsesRepository()
        {
            Driver driver = new() { Id = 1 };
            Mock<IDriverRepository> repoMock = new();
            repoMock.Setup(r => r.GetDriverByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
            DriverService service = new(repoMock.Object);

            Driver? result = await service.GetDriverByIdAsync(1);

            Assert.Equal(driver, result);
            repoMock.Verify(r => r.GetDriverByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateDriverAsync_CallsRepository()
        {
            Driver newDriver = new() { Id = 1 };
            Mock<IDriverRepository> repoMock = new();
            repoMock.Setup(r => r.CreateDriverAsync(newDriver, It.IsAny<CancellationToken>())).ReturnsAsync(newDriver);
            DriverService service = new(repoMock.Object);
            Driver result = await service.CreateDriverAsync(newDriver);

            Assert.Equal(newDriver, result);
            repoMock.Verify(r => r.CreateDriverAsync(newDriver, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class NotificationServiceTests
    {
        [Fact]
        public async Task SendSmsAsync_CallsCommunicationService()
        {
            Mock<IDistributedCache> cacheMock = new();
            Mock<ILogger<NotificationService>> loggerMock = new();
            Mock<ICommunicationServiceFactory> factoryMock = new();
            Mock<ICommunicationService> commServiceMock = new();

            commServiceMock.Setup(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            factoryMock.Setup(f => f.GetService("Sms")).Returns(commServiceMock.Object);

            NotificationService service = new(cacheMock.Object, loggerMock.Object, factoryMock.Object);

            bool result = await service.SendSmsAsync("+1234567890", "Test message");

            Assert.True(result);
            factoryMock.Verify(f => f.GetService("Sms"), Times.Once);
            commServiceMock.Verify(
                s => s.SendAsync(It.Is<CommunicationMessage>(m => m.To == "+1234567890" && m.Body == "Test message"),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_CallsCommunicationService()
        {
            Mock<IDistributedCache> cacheMock = new();
            Mock<ILogger<NotificationService>> loggerMock = new();
            Mock<ICommunicationServiceFactory> factoryMock = new();
            Mock<ICommunicationService> commServiceMock = new();

            commServiceMock.Setup(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            factoryMock.Setup(f => f.GetService("Email")).Returns(commServiceMock.Object);

            NotificationService service = new(cacheMock.Object, loggerMock.Object, factoryMock.Object);

            bool result = await service.SendEmailAsync("test@example.com", "Subject", "Test message");

            Assert.True(result);
            factoryMock.Verify(f => f.GetService("Email"), Times.Once);
            commServiceMock.Verify(
                s => s.SendAsync(
                    It.Is<CommunicationMessage>(m =>
                        m.To == "test@example.com" && m.Subject == "Subject" && m.Body == "Test message"),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendPushNotificationAsync_CallsCommunicationService()
        {
            Mock<IDistributedCache> cacheMock = new();
            Mock<ILogger<NotificationService>> loggerMock = new();
            Mock<ICommunicationServiceFactory> factoryMock = new();
            Mock<ICommunicationService> commServiceMock = new();

            commServiceMock.Setup(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            factoryMock.Setup(f => f.GetService("Push")).Returns(commServiceMock.Object);

            NotificationService service = new(cacheMock.Object, loggerMock.Object, factoryMock.Object);

            bool result = await service.SendPushNotificationAsync("device123", "Title", "Test message");

            Assert.True(result);
            factoryMock.Verify(f => f.GetService("Push"), Times.Once);
            commServiceMock.Verify(
                s => s.SendAsync(
                    It.Is<CommunicationMessage>(m =>
                        m.To == "device123" && m.Title == "Title" && m.Body == "Test message"),
                    It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendSmsAsync_RetryOnFailure()
        {
            Mock<IDistributedCache> cacheMock = new();
            Mock<ILogger<NotificationService>> loggerMock = new();
            Mock<ICommunicationServiceFactory> factoryMock = new();
            Mock<ICommunicationService> commServiceMock = new();

            // Fail first two times, succeed on third
            commServiceMock.SetupSequence(s =>
                    s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .ReturnsAsync(false)
                .ReturnsAsync(true);
            factoryMock.Setup(f => f.GetService("Sms")).Returns(commServiceMock.Object);

            NotificationService service = new(cacheMock.Object, loggerMock.Object, factoryMock.Object);

            bool result = await service.SendSmsAsync("+1234567890", "Test message");

            Assert.True(result);
            commServiceMock.Verify(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>()),
                Times.Exactly(3));
        }
    }

    public class JobServiceTests
    {
        [Fact]
        public async Task GetJobsAsync_ReturnsJobsFromRepository()
        {
            List<Job> expected = [new() { Id = "1" }, new() { Id = "2" }];
            Mock<IJobRepository> repoMock = new();
            Mock<ICustomerRepository> customerRepoMock = new();
            Mock<IDriverRepository> driverRepoMock = new();
            Mock<INotificationService> notificationServiceMock = new();
            Mock<IMappingService> mappingServiceMock = new();
            Mock<ILogger<JobService>> loggerMock = new();
            repoMock.Setup(r => r.GetJobsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            JobService service = new(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object,
                notificationServiceMock.Object, mappingServiceMock.Object, Mock.Of<IRealtimeService>(),
                loggerMock.Object);

            IEnumerable<Job> result = await service.GetJobsAsync(CancellationToken.None);

            Assert.Equal(expected, result);
            repoMock.Verify(r => r.GetJobsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetJobByIdAsync_UsesRepository()
        {
            Job job = new() { Id = "1" };
            Mock<IJobRepository> repoMock = new();
            Mock<ICustomerRepository> customerRepoMock = new();
            Mock<IDriverRepository> driverRepoMock = new();
            Mock<INotificationService> notificationServiceMock = new();
            Mock<IMappingService> mappingServiceMock = new();
            Mock<ILogger<JobService>> loggerMock = new();
            repoMock.Setup(r => r.GetJobByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(job);
            JobService service = new(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object,
                notificationServiceMock.Object, mappingServiceMock.Object, Mock.Of<IRealtimeService>(),
                loggerMock.Object);

            Job? result = await service.GetJobByIdAsync("1");

            Assert.Equal(job, result);
            repoMock.Verify(r => r.GetJobByIdAsync("1", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateJobAsync_CallsRepository()
        {
            Job job = new()
            {
                Id = "1",
                CustomerId = "CUST001",
                Title = "Test Job",
                PickupLocation = new Location
                {
                    Address = "123 Pickup St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Latitude = 40.7128m,
                    Longitude = -74.0060m
                },
                DeliveryLocation = new Location
                {
                    Address = "456 Delivery Ave",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10002",
                    Latitude = 40.7589m,
                    Longitude = -73.9851m
                },
                Cargo = new CargoDetails
                {
                    Description = "Test cargo",
                    Weight = 100,
                    Value = 1000,
                    Quantity = 1
                },
                Pricing = new PricingDetails
                {
                    BaseRate = 100,
                    MileageRate = 2.5m,
                    FuelSurcharge = 10,
                    TollCharges = 5,
                    AdditionalCharges = 0,
                    TotalAmount = 115,
                    Discount = 0,
                    Tax = 0,
                    TaxRate = 0
                },
                ScheduledPickupTime = DateTime.UtcNow.AddHours(2),
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(8)
            };

            Customer customer = new() { Id = "CUST001", IsActive = true };

            Mock<IJobRepository> repoMock = new();
            Mock<ICustomerRepository> customerRepoMock = new();
            Mock<IDriverRepository> driverRepoMock = new();
            Mock<INotificationService> notificationServiceMock = new();
            Mock<IMappingService> mappingServiceMock = new();
            Mock<ILogger<JobService>> loggerMock = new();

            // Add this setup inside the test
            mappingServiceMock
                .Setup(m => m.ReverseGeocodeAsync(It.IsAny<decimal>(), It.IsAny<decimal>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReverseGeocodingResult { IsValid = true });

            repoMock.Setup(r => r.CreateJobAsync(job, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            customerRepoMock.Setup(r => r.GetByIdAsync("CUST001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            JobService service = new(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object,
                notificationServiceMock.Object, mappingServiceMock.Object, Mock.Of<IRealtimeService>(),
                loggerMock.Object);

            Job result = await service.CreateJobAsync(job);

            Assert.Equal(job, result);
            repoMock.Verify(r => r.CreateJobAsync(job, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateJobAsync_CallsRepository()
        {
            Job job = new()
            {
                Id = "1",
                CustomerId = "CUST001",
                Title = "Test Job",
                PickupLocation = new Location
                {
                    Address = "123 Pickup St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Latitude = 40.7128m,
                    Longitude = -74.0060m
                },
                DeliveryLocation = new Location
                {
                    Address = "456 Delivery Ave",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10002",
                    Latitude = 40.7589m,
                    Longitude = -73.9851m
                },
                Cargo = new CargoDetails
                {
                    Description = "Test cargo",
                    Weight = 100,
                    Value = 1000,
                    Quantity = 1
                },
                Pricing = new PricingDetails
                {
                    BaseRate = 100,
                    MileageRate = 2.5m,
                    FuelSurcharge = 10,
                    TollCharges = 5,
                    AdditionalCharges = 0,
                    TotalAmount = 115,
                    Discount = 0,
                    Tax = 0,
                    TaxRate = 0
                },
                ScheduledPickupTime = DateTime.UtcNow.AddHours(2),
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(8)
            };
            Mock<IJobRepository> repoMock = new();
            Mock<ICustomerRepository> customerRepoMock = new();
            Mock<IDriverRepository> driverRepoMock = new();
            Mock<INotificationService> notificationServiceMock = new();
            Mock<IMappingService> mappingServiceMock = new();
            Mock<ILogger<JobService>> loggerMock = new();
            repoMock.Setup(r => r.GetJobByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(job);
            repoMock.Setup(r => r.UpdateJobAsync("1", job, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            JobService service = new(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object,
                notificationServiceMock.Object, mappingServiceMock.Object, Mock.Of<IRealtimeService>(),
                loggerMock.Object);

            Job? result = await service.UpdateJobAsync("1", job);

            Assert.Equal(job, result);
            repoMock.Verify(r => r.UpdateJobAsync("1", job, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteJobAsync_CallsRepository()
        {
            Job job = new() { Id = "1", Status = JobStatus.Pending };
            Mock<IJobRepository> repoMock = new();
            Mock<ICustomerRepository> customerRepoMock = new();
            Mock<IDriverRepository> driverRepoMock = new();
            Mock<INotificationService> notificationServiceMock = new();
            Mock<IMappingService> mappingServiceMock = new();
            Mock<ILogger<JobService>> loggerMock = new();
            repoMock.Setup(r => r.GetJobByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(job);
            repoMock.Setup(r => r.DeleteJobAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
            JobService service = new(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object,
                notificationServiceMock.Object, mappingServiceMock.Object, Mock.Of<IRealtimeService>(),
                loggerMock.Object);

            bool result = await service.DeleteJobAsync("1", It.IsAny<CancellationToken>());

            Assert.True(result);
            repoMock.Verify(r => r.DeleteJobAsync("1", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
