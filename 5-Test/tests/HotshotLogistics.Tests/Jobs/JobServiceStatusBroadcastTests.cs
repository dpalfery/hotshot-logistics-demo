using FluentAssertions;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Jobs
{
    /// <summary>
    ///     Every path through <see cref="JobService" /> that changes a job's status tells the dashboard once.
    /// </summary>
    public class JobServiceStatusBroadcastTests
    {
        private readonly Mock<IDriverRepository> _driverRepository = new();
        private readonly Mock<IJobRepository> _jobRepository = new();
        private readonly Mock<ILogger<JobService>> _logger = new();
        private readonly Mock<IRealtimeService> _realtimeService = new();
        private readonly JobService _service;

        public JobServiceStatusBroadcastTests()
        {
            Mock<ICustomerRepository> customerRepository = new();
            customerRepository.Setup(r => r.GetByIdAsync("CUST001", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer { Id = "CUST001", IsActive = true });
            Mock<IMappingService> mappingService = new();
            mappingService
                .Setup(m => m.ReverseGeocodeAsync(It.IsAny<decimal>(), It.IsAny<decimal>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReverseGeocodingResult { IsValid = true });

            _service = new JobService(_jobRepository.Object, customerRepository.Object, _driverRepository.Object,
                Mock.Of<INotificationService>(), mappingService.Object, _realtimeService.Object, _logger.Object);
        }

        [Fact]
        public async Task CreateJobAsync_BroadcastsThePendingJobOnce()
        {
            Job job = NewJob(JobStatus.Pending);
            _jobRepository.Setup(r => r.CreateJobAsync(job, It.IsAny<CancellationToken>())).ReturnsAsync(job);

            await _service.CreateJobAsync(job);

            VerifyBroadcastOnce(job.Id, JobStatus.Pending);
        }

        [Fact]
        public async Task UpdateJobAsync_WhenTheStatusChanges_BroadcastsTheNewStatusOnce()
        {
            Job stored = NewJob(JobStatus.Pending);
            Job changes = NewJob(JobStatus.EnRoute, stored.Id);
            _jobRepository.Setup(r => r.GetJobByIdAsync(stored.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stored);
            _jobRepository.Setup(r => r.UpdateJobAsync(stored.Id, changes, It.IsAny<CancellationToken>()))
                .ReturnsAsync(changes);

            await _service.UpdateJobAsync(stored.Id, changes);

            VerifyBroadcastOnce(stored.Id, JobStatus.EnRoute);
        }

        [Fact]
        public async Task UpdateJobAsync_WhenTheStatusIsUnchanged_BroadcastsNothing()
        {
            Job stored = NewJob(JobStatus.Assigned);
            Job changes = NewJob(JobStatus.Assigned, stored.Id);
            _jobRepository.Setup(r => r.GetJobByIdAsync(stored.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stored);
            _jobRepository.Setup(r => r.UpdateJobAsync(stored.Id, changes, It.IsAny<CancellationToken>()))
                .ReturnsAsync(changes);

            await _service.UpdateJobAsync(stored.Id, changes);

            _realtimeService.Verify(r => r.BroadcastJobStatusUpdate(It.IsAny<string>(), It.IsAny<JobStatus>()),
                Times.Never);
        }

        [Fact]
        public async Task AssignDriverAsync_BroadcastsAssignedOnce()
        {
            Job job = NewJob(JobStatus.Pending);
            _jobRepository.Setup(r => r.GetJobByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            _jobRepository.Setup(r => r.UpdateJobAsync(job.Id, job, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _jobRepository.Setup(r => r.GetByDriverIdAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Job>());
            _driverRepository.Setup(r => r.GetDriverByIdAsync(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Driver { Id = 7, IsActive = true });

            await _service.AssignDriverAsync(job.Id, 7);

            VerifyBroadcastOnce(job.Id, JobStatus.Assigned);
        }

        [Theory]
        [InlineData(JobStatus.Assigned)]
        [InlineData(JobStatus.EnRoute)]
        [InlineData(JobStatus.Received)]
        public async Task UpdateJobStatusAsync_BroadcastsTheNewStatusOnce(JobStatus status)
        {
            Job job = NewJob(JobStatus.Pending);
            _jobRepository.Setup(r => r.GetJobByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            _jobRepository.Setup(r => r.UpdateJobAsync(job.Id, job, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            await _service.UpdateJobStatusAsync(job.Id, status);

            VerifyBroadcastOnce(job.Id, status);
        }

        [Fact]
        public async Task UpdateJobStatusAsync_WhenTheJobCannotBeSaved_BroadcastsNothing()
        {
            Job job = NewJob(JobStatus.Pending);
            _jobRepository.Setup(r => r.GetJobByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            _jobRepository.Setup(r => r.UpdateJobAsync(job.Id, job, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Job?)null);

            Func<Task> act = () => _service.UpdateJobStatusAsync(job.Id, JobStatus.EnRoute);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _realtimeService.Verify(r => r.BroadcastJobStatusUpdate(It.IsAny<string>(), It.IsAny<JobStatus>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateJobStatusAsync_WhenTheBroadcastFails_StillReturnsTheSavedJobAndLogsAWarning()
        {
            Job job = NewJob(JobStatus.Pending);
            _jobRepository.Setup(r => r.GetJobByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            _jobRepository.Setup(r => r.UpdateJobAsync(job.Id, job, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _realtimeService.Setup(r => r.BroadcastJobStatusUpdate(job.Id, JobStatus.EnRoute))
                .ThrowsAsync(new InvalidOperationException("hub unavailable"));

            Job result = await _service.UpdateJobStatusAsync(job.Id, JobStatus.EnRoute);

            result.Should().BeSameAs(job);
            _logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v!.ToString()!.Contains(job.Id)),
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private void VerifyBroadcastOnce(string jobId, JobStatus status)
        {
            _realtimeService.Verify(r => r.BroadcastJobStatusUpdate(jobId, status), Times.Once);
            _realtimeService.Verify(r => r.BroadcastJobStatusUpdate(It.IsAny<string>(), It.IsAny<JobStatus>()),
                Times.Once);
        }

        private static Job NewJob(JobStatus status, string? id = null)
        {
            return new Job
            {
                Id = id ?? $"test-job-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                Title = "Status broadcast job",
                Status = status,
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
                Cargo = new CargoDetails { Description = "Test cargo", Weight = 100, Value = 1000, Quantity = 1 },
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
        }
    }
}
