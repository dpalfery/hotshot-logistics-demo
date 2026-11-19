// <copyright file="DataIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Utils.Integration
{
    /// <summary>
    ///     Unit/integration-style tests adapted to use repository interfaces and mocks (no EF).
    ///     These tests validate service behaviour and repository interactions without relying on Entity Framework.
    /// </summary>
    public class DataIntegrationTests
    {
        [Fact]
        public async Task DriverService_CreateDriver_CallsRepository()
        {
            Mock<IDriverRepository> mockRepo = new(MockBehavior.Strict);
            Driver driver = new()
            {
                PersonalInfo = new PersonalInfo
                {
                    FirstName = "Test",
                    LastName = "Driver",
                    Email = "test.driver@example.com",
                    PhoneNumber = "555-0000"
                },
                License = new LicenseInfo
                {
                    LicenseNumber = "T1234567",
                    LicenseExpiryDate = DateTime.UtcNow.AddYears(5)
                },
                IsActive = true
            };

            mockRepo.Setup(r => r.CreateDriverAsync(It.IsAny<Driver>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Driver d, CancellationToken _) => d)
                .Verifiable();

            DriverService service = new(mockRepo.Object);

            Driver created = await service.CreateDriverAsync(driver);

            Assert.NotNull(created);
            mockRepo.Verify(r => r.CreateDriverAsync(It.Is<Driver>(x => x == driver), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task JobService_CreateJob_CallsRepository()
        {
            Mock<IJobRepository> mockRepo = new(MockBehavior.Strict);
            Job newJob = new()
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = "CUST001",
                Title = "Integration Test Job",
                Status = JobStatus.Pending,
                Priority = JobPriority.Low,
                Amount = 10.0m,
                PickupLocation = new Location
                {
                    Address = "Test Pickup",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Latitude = 40.7128m,
                    Longitude = -74.0060m
                },
                DeliveryLocation = new Location
                {
                    Address = "Test Dropoff",
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

            mockRepo.Setup(r => r.CreateJobAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Job j, CancellationToken _) => j)
                .Verifiable();

            Mock<ICustomerRepository> customerRepoMock = new();
            Mock<IDriverRepository> driverRepoMock = new();
            Mock<INotificationService> notificationServiceMock = new();
            Mock<IMappingService> mappingServiceMock = new();
            Mock<ILogger<JobService>> loggerMock = new();

            // Precise Moq setup so validation passes:
            mappingServiceMock
                .Setup(m => m.ReverseGeocodeAsync(It.IsAny<decimal>(), It.IsAny<decimal>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReverseGeocodingResult { IsValid = true });

            mappingServiceMock
                .Setup(m => m.GeocodeAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GeocodingResult { IsValid = true, Latitude = 40.7128m, Longitude = -74.0060m });

            customerRepoMock.Setup(r => r.GetByIdAsync("CUST001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            JobService service = new(mockRepo.Object, customerRepoMock.Object, driverRepoMock.Object,
                notificationServiceMock.Object, mappingServiceMock.Object, Mock.Of<IRealtimeService>(),
                loggerMock.Object);

            Job created = await service.CreateJobAsync(newJob);

            Assert.NotNull(created);
            mockRepo.Verify(r => r.CreateJobAsync(It.Is<Job>(x => x == newJob), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task JobAssignmentService_AssignJobAsync_CreatesAssignment()
        {
            Mock<IJobAssignmentRepository> mockAssignmentRepo = new(MockBehavior.Strict);
            Mock<IJobRepository> mockJobRepo = new(MockBehavior.Strict);
            Mock<IDriverRepository> mockDriverRepo = new(MockBehavior.Strict);

            string jobId = Guid.NewGuid().ToString();
            int driverId = 123;

            Job existingJob = new() { Id = jobId, Title = "Job A" };
            Driver existingDriver = new()
            { Id = driverId, PersonalInfo = new PersonalInfo { FirstName = "Alice", LastName = "Smith" } };

            mockJobRepo.Setup(r => r.GetJobByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingJob)
                .Verifiable();

            mockDriverRepo.Setup(r => r.GetDriverByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingDriver)
                .Verifiable();

            mockAssignmentRepo.Setup(r => r.GetByJobIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<JobAssignmentDto>())
                .Verifiable();

            mockAssignmentRepo.Setup(r => r.CreateAsync(It.IsAny<JobAssignmentDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((JobAssignmentDto a, CancellationToken _) => a)
                .Verifiable();

            JobAssignmentService assignmentService = new(
                mockAssignmentRepo.Object,
                mockJobRepo.Object,
                mockDriverRepo.Object);

            JobAssignmentDto result = await assignmentService.AssignJobAsync(jobId, driverId, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(jobId, result.JobId);
            Assert.Equal(driverId, result.DriverId);
            Assert.Equal(JobAssignmentStatus.Active, result.Status);

            mockJobRepo.Verify(r => r.GetJobByIdAsync(It.Is<string>(s => s == jobId), It.IsAny<CancellationToken>()),
                Times.Once);
            mockDriverRepo.Verify(
                r => r.GetDriverByIdAsync(It.Is<int>(d => d == driverId), It.IsAny<CancellationToken>()), Times.Once);
            mockAssignmentRepo.Verify(r => r.CreateAsync(It.IsAny<JobAssignmentDto>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
