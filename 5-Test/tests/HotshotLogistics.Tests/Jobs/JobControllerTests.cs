// <copyright file="JobControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Reflection;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HotshotLogistics.Api.Controllers;
using HotshotLogistics.Api.Models;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Jobs
{
    /// <summary>
    ///     Integration tests for the JobController.
    /// </summary>
    public class JobControllerTests
    {
        private readonly JobController _controller;
        private readonly Mock<IJobRepository> _mockJobRepository;
        private readonly Mock<IJobService> _mockJobService;
        private readonly Mock<IValidator<Job>> _mockJobValidator;

        /// <summary>
        ///     Initializes a new instance of the <see cref="JobControllerTests" /> class.
        /// </summary>
        public JobControllerTests()
        {
            _mockJobService = new Mock<IJobService>();
            _mockJobRepository = new Mock<IJobRepository>();
            _mockJobValidator = new Mock<IValidator<Job>>();
            Mock<ILogger<JobController>> mockLogger = new();
            _controller = new JobController(_mockJobService.Object, _mockJobRepository.Object, mockLogger.Object,
                _mockJobValidator.Object);
        }

        /// <summary>
        ///     Tests that GetJobs returns paged results with filtering.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobs_WithFiltering_ReturnsPagedResults()
        {
            // Arrange
            List<Job> expectedJobs =
            [
                CreateTestJob("job1", JobStatus.Pending),
                CreateTestJob("job2", JobStatus.Assigned)
            ];

            PagedResult<Job> pagedResult = new()
            {
                Items = expectedJobs,
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            _mockJobRepository.Setup(r => r.GetJobsAsync(
                    It.IsAny<JobFilterDto>(),
                    It.IsAny<PaginationParameters>(),
                    It.IsAny<SortParameters>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            ActionResult<PagedResult<Job>> result = await _controller.GetJobs(
                JobStatus.Pending,
                pageNumber: 1,
                pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            PagedResult<Job> returnedResult = okResult.Value.Should().BeOfType<PagedResult<Job>>().Subject;
            returnedResult.Items.Should().HaveCount(2);
            returnedResult.TotalCount.Should().Be(2);
        }

        /// <summary>
        ///     Tests that GetJobs passes the active-jobs filter to the repository, so a client can count active jobs
        ///     on the server.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobs_WithIsActive_PassesTheActiveFilterToTheRepository()
        {
            // Arrange
            JobFilterDto? captured = null;
            _mockJobRepository.Setup(r => r.GetJobsAsync(
                    It.IsAny<JobFilterDto>(),
                    It.IsAny<PaginationParameters>(),
                    It.IsAny<SortParameters>(),
                    It.IsAny<CancellationToken>()))
                .Callback<JobFilterDto?, PaginationParameters?, SortParameters?, CancellationToken>(
                    (filter, _, _, _) => captured = filter)
                .ReturnsAsync(new PagedResult<Job> { Items = [], TotalCount = 7, PageNumber = 1, PageSize = 1 });

            // Act
            ActionResult<PagedResult<Job>> result = await _controller.GetJobs(isActive: true, pageSize: 1);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            captured.Should().NotBeNull();
            captured.IsActive.Should().BeTrue();
        }

        /// <summary>
        ///     Tests that GetJobById returns the job when found.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobById_WhenJobExists_ReturnsJob()
        {
            // Arrange
            string jobId = "test-job-id";
            Job expectedJob = CreateTestJob(jobId, JobStatus.Pending);

            _mockJobService.Setup(s => s.GetJobByIdAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedJob);

            // Act
            ActionResult<Job> result = await _controller.GetJobById(jobId);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            Job returnedJob = okResult.Value.Should().BeAssignableTo<Job>().Subject;
            returnedJob.Id.Should().Be(jobId);
        }

        /// <summary>
        ///     Tests that GetJobById returns NotFound when job doesn't exist.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobById_WhenJobNotFound_ReturnsNotFound()
        {
            // Arrange
            string jobId = "non-existent-job";

            _mockJobService.Setup(s => s.GetJobByIdAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Job?)null);

            // Act
            ActionResult<Job> result = await _controller.GetJobById(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        ///     Tests that CreateJob creates and returns the new job.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CreateJob_WithValidData_CreatesAndReturnsJob()
        {
            // Arrange
            Job jobDto = new()
            {
                Id = "new-job-id",
                Title = "Test Job",
                CustomerId = "customer1",
                Status = JobStatus.Pending,
                Priority = JobPriority.Medium,
                Amount = 100.00m,
                ScheduledPickupTime = DateTime.UtcNow.AddHours(2),
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(8) // <-- FIXED HERE
            };

            Job createdJob = CreateTestJob(jobDto.Id, jobDto.Status);

            _mockJobValidator.Setup(v => v.ValidateAsync(jobDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _mockJobService.Setup(s => s.CreateJobAsync(jobDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdJob);

            // Act
            ActionResult<Job> result = await _controller.CreateJob(jobDto);

            // Assert
            result.Should().NotBeNull();
            CreatedAtActionResult createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.Value.Should().NotBeNull();
            object responseData = createdResult.Value;
            PropertyInfo? idProperty = responseData.GetType().GetProperty("Id");
            idProperty.Should().NotBeNull();
            idProperty.GetValue(responseData).Should().Be(jobDto.Id);
        }

        /// <summary>
        ///     Tests that CreateJob returns BadRequest when job data is null.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CreateJob_WithNullData_ReturnsBadRequest()
        {
            // Act
            ActionResult<Job> result = await _controller.CreateJob(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that CreateJob returns BadRequest when validation fails.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CreateJob_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            Job jobDto = new()
            {
                Id = "invalid-job",
                Title = "", // Invalid: empty title
                CustomerId = "customer1"
            };

            // Setup validator to return validation failures
            List<ValidationFailure> validationFailures =
            [
                new("Title", "Job title is required.")
            ];
            ValidationResult validationResult = new(validationFailures);

            _mockJobValidator.Setup(v => v.ValidateAsync(jobDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            ActionResult<Job> result = await _controller.CreateJob(jobDto);

            // Assert
            result.Should().NotBeNull();
            BadRequestObjectResult badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            ValidationProblemDetails problem =
                badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
            problem.Status.Should().Be(400);
            problem.Errors.Should().ContainKey("Title").WhoseValue.Should().Equal("Job title is required.");
        }

        /// <summary>
        ///     Tests that UpdateJob updates and returns the job.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateJob_WithValidData_UpdatesAndReturnsJob()
        {
            // Arrange
            string jobId = "existing-job-id";
            Job jobDto = new()
            {
                Id = jobId,
                Title = "Updated Job Title",
                CustomerId = "customer1",
                Status = JobStatus.Assigned,
                Priority = JobPriority.High,
                Amount = 150.00m
            };

            Job updatedJob = CreateTestJob(jobId, JobStatus.Assigned);

            _mockJobValidator.Setup(v => v.ValidateAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _mockJobService.Setup(s => s.UpdateJobAsync(jobId, It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedJob);

            // Act
            ActionResult<Job> result = await _controller.UpdateJob(jobId, jobDto);

            // Assert
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeSameAs(updatedJob);
        }

        /// <summary>
        ///     Tests that UpdateJob returns NotFound when job doesn't exist.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateJob_WhenJobNotFound_ReturnsNotFound()
        {
            // Arrange
            string jobId = "non-existent-job";
            Job jobDto = new() { Id = jobId, Title = "Test" };

            _mockJobValidator.Setup(v => v.ValidateAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _mockJobService.Setup(s => s.UpdateJobAsync(jobId, It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Job?)null);

            // Act
            ActionResult<Job> result = await _controller.UpdateJob(jobId, jobDto);

            // Assert
            NotFoundObjectResult notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            ProblemDetails problem = notFound.Value.Should().BeOfType<ProblemDetails>().Subject;
            problem.Status.Should().Be(404);
            problem.Detail.Should().Contain(jobId);
        }

        /// <summary>
        ///     Tests that UpdateJob returns a validation problem when the job fails validation.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateJob_WithInvalidData_ReturnsValidationProblem()
        {
            // Arrange
            Job jobDto = new() { Id = "job-1", Title = string.Empty };
            _mockJobValidator.Setup(v => v.ValidateAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult([new ValidationFailure("Title", "Job title is required.")]));

            // Act
            ActionResult<Job> result = await _controller.UpdateJob("job-1", jobDto);

            // Assert
            BadRequestObjectResult badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            ValidationProblemDetails problem = badRequest.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
            problem.Errors.Should().ContainKey("Title");
        }

        /// <summary>
        ///     Tests that an unexpected service failure reaches the exception handler instead of being swallowed.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateJob_WhenServiceThrowsUnexpectedly_PropagatesToTheExceptionHandler()
        {
            // Arrange
            _mockJobValidator.Setup(v => v.ValidateAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _mockJobService.Setup(s => s.UpdateJobAsync("job-1", It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException("database timed out"));

            // Act
            Func<Task> act = () => _controller.UpdateJob("job-1", new Job { Id = "job-1", Title = "Title" });

            // Assert
            await act.Should().ThrowAsync<TimeoutException>();
        }

        /// <summary>
        ///     Tests that DeleteJob deletes the job and returns NoContent.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task DeleteJob_WhenJobExists_ReturnsNoContent()
        {
            // Arrange
            string jobId = "job-to-delete";

            _mockJobService.Setup(s => s.DeleteJobAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            IActionResult result = await _controller.DeleteJob(jobId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        /// <summary>
        ///     Tests that DeleteJob returns NotFound when job doesn't exist.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task DeleteJob_WhenJobNotFound_ReturnsNotFound()
        {
            // Arrange
            string jobId = "non-existent-job";

            _mockJobService.Setup(s => s.DeleteJobAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.DeleteJob(jobId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        ///     Tests that DeleteJob returns BadRequest when job cannot be deleted.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task DeleteJob_WhenJobCannotBeDeleted_ReturnsBadRequest()
        {
            // Arrange
            string jobId = "job-in-progress";

            _mockJobService.Setup(s => s.DeleteJobAsync(jobId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Cannot delete job with status: InProgress"));

            // Act
            IActionResult result = await _controller.DeleteJob(jobId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that AssignDriver assigns driver and returns updated job.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task AssignDriver_WithValidRequest_ReturnsUpdatedJob()
        {
            // Arrange
            string jobId = "job-to-assign";
            int driverId = 123;
            AssignDriverRequest request = new() { DriverId = driverId };
            Job updatedJob = CreateTestJob(jobId, JobStatus.Assigned);
            updatedJob.AssignedDriverId = driverId;

            _mockJobService.Setup(s => s.AssignDriverAsync(jobId, driverId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedJob);

            // Act
            ActionResult<Job> result = await _controller.AssignDriver(jobId, request);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            Job returnedJob = okResult.Value.Should().BeAssignableTo<Job>().Subject;
            returnedJob.AssignedDriverId.Should().Be(driverId);
        }

        /// <summary>
        ///     Tests that AssignDriver returns Conflict when driver is not available.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task AssignDriver_WhenDriverNotAvailable_ReturnsConflict()
        {
            // Arrange
            string jobId = "job-to-assign";
            int driverId = 123;
            AssignDriverRequest request = new() { DriverId = driverId };

            _mockJobService.Setup(s => s.AssignDriverAsync(jobId, driverId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Driver is not available"));

            // Act
            ActionResult<Job> result = await _controller.AssignDriver(jobId, request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<ConflictObjectResult>();
        }

        /// <summary>
        ///     Tests that UpdateJobStatus updates status and returns updated job.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateJobStatus_WithValidRequest_ReturnsUpdatedJob()
        {
            // Arrange
            string jobId = "job-to-update";
            JobStatus newStatus = JobStatus.EnRoute;
            UpdateJobStatusRequest request = new() { Status = newStatus };
            Job updatedJob = CreateTestJob(jobId, newStatus);

            _mockJobService.Setup(s => s.UpdateJobStatusAsync(jobId, newStatus, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedJob);

            // Act
            ActionResult<Job> result = await _controller.UpdateJobStatus(jobId, request);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            Job returnedJob = okResult.Value.Should().BeAssignableTo<Job>().Subject;
            returnedJob.Status.Should().Be(newStatus);
        }

        /// <summary>
        ///     Tests that GetJobsByStatus returns jobs with specified status.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobsByStatus_ReturnsJobsWithStatus()
        {
            // Arrange
            JobStatus status = JobStatus.Pending;
            List<Job> expectedJobs =
            [
                CreateTestJob("job1", status),
                CreateTestJob("job2", status)
            ];

            _mockJobRepository.Setup(r => r.GetJobsByStatusAsync(status, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedJobs);

            // Act
            ActionResult<IEnumerable<Job>> result = await _controller.GetJobsByStatus(status);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            List<Job> returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<Job>>().Subject.ToList();
            returnedJobs.Should().HaveCount(2);
            returnedJobs.All(j => j.Status == status).Should().BeTrue();
        }

        /// <summary>
        ///     Tests that GetJobsByDriver returns jobs assigned to driver.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobsByDriver_ReturnsJobsForDriver()
        {
            // Arrange
            int driverId = 123;
            List<Job> expectedJobs =
            [
                CreateTestJob("job1", JobStatus.Assigned, driverId),
                CreateTestJob("job2", JobStatus.EnRoute, driverId)
            ];

            _mockJobRepository.Setup(r => r.GetJobsByDriverAsync(driverId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedJobs);

            // Act
            ActionResult<IEnumerable<Job>> result = await _controller.GetJobsByDriver(driverId);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            List<Job> returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<Job>>().Subject.ToList();
            returnedJobs.Should().HaveCount(2);
            returnedJobs.All(j => j.AssignedDriverId == driverId).Should().BeTrue();
        }

        /// <summary>
        ///     Tests that GetJobsByCustomer returns jobs for customer.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobsByCustomer_ReturnsJobsForCustomer()
        {
            // Arrange
            string customerId = "customer123";
            List<Job> expectedJobs =
            [
                CreateTestJob("job1", JobStatus.Pending, customerId: customerId),
                CreateTestJob("job2", JobStatus.Received, customerId: customerId)
            ];

            _mockJobRepository.Setup(r => r.GetJobsByCustomerAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedJobs);

            // Act
            ActionResult<IEnumerable<Job>> result = await _controller.GetJobsByCustomer(customerId);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            List<Job> returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<Job>>().Subject.ToList();
            returnedJobs.Should().HaveCount(2);
            returnedJobs.All(j => j.CustomerId == customerId).Should().BeTrue();
        }

        /// <summary>
        ///     Tests that GetOverdueJobs returns overdue jobs.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetOverdueJobs_ReturnsOverdueJobs()
        {
            // Arrange
            List<Job> overdueJobs =
            [
                CreateTestJob("overdue1", JobStatus.EnRoute),
                CreateTestJob("overdue2", JobStatus.Assigned)
            ];

            _mockJobRepository.Setup(r => r.GetOverdueJobsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(overdueJobs);

            // Act
            ActionResult<IEnumerable<Job>> result = await _controller.GetOverdueJobs();

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            List<Job> returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<Job>>().Subject.ToList();
            returnedJobs.Should().HaveCount(2);
        }

        /// <summary>
        ///     Creates a test job for testing purposes.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <param name="status">The job status.</param>
        /// <param name="driverId">The optional driver ID.</param>
        /// <param name="customerId">The optional customer ID.</param>
        /// <returns>A test job instance.</returns>
        private static Job CreateTestJob(string id, JobStatus status, int? driverId = null, string? customerId = null)
        {
            return new Job
            {
                Id = id,
                Title = $"Test Job {id}",
                CustomerId = customerId ?? "test-customer",
                Status = status,
                Priority = JobPriority.Medium,
                Amount = 100.00m,
                AssignedDriverId = driverId,
                CreatedAt = DateTime.UtcNow,
                ScheduledPickupTime = DateTime.UtcNow.AddHours(2),
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(8), // <-- FIXED HERE
                PickupLocation = new Location { Address = "123 Pickup St", Latitude = 40.7128m, Longitude = -74.0060m },
                DeliveryLocation = new Location
                { Address = "456 Delivery Ave", Latitude = 40.7589m, Longitude = -73.9851m },
                Cargo = new CargoDetails { Description = "Test cargo", Weight = 100, Value = 1000 },
                Pricing = new PricingDetails { BaseRate = 100, TotalAmount = 100 },
                Documents = new List<JobDocument>(),
                Tracking = new TrackingInfo { CurrentStatus = "Created", IsActive = false }
            };
        }
    }
}
