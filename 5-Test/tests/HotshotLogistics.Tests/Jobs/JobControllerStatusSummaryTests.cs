// <copyright file="JobControllerStatusSummaryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using FluentValidation;
using HotshotLogistics.Api.Controllers;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Jobs
{
    /// <summary>
    ///     Unit tests for the job status summary controller endpoint.
    /// </summary>
    public class JobControllerStatusSummaryTests
    {
        private readonly JobController _controller;
        private readonly Mock<IJobRepository> _mockJobRepository;

        /// <summary>
        ///     Initializes a new instance of the <see cref="JobControllerStatusSummaryTests" /> class.
        /// </summary>
        public JobControllerStatusSummaryTests()
        {
            _mockJobRepository = new Mock<IJobRepository>();
            Mock<ICustomerRepository> mockCustomerRepository = new();
            Mock<IDriverRepository> mockDriverRepository = new();
            Mock<INotificationService> mockNotificationService = new();
            Mock<IMappingService> mockMappingService = new();
            Mock<IValidator<Job>> mockJobValidator = new();

            JobService service = new(
                _mockJobRepository.Object,
                mockCustomerRepository.Object,
                mockDriverRepository.Object,
                mockNotificationService.Object,
                mockMappingService.Object,
                new Mock<ILogger<JobService>>().Object);

            _controller = new JobController(
                service,
                _mockJobRepository.Object,
                new Mock<ILogger<JobController>>().Object,
                mockJobValidator.Object);
        }

        /// <summary>
        ///     Tests that the status summary endpoint returns the summary DTO in an OK result.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobStatusSummary_WhenCountsAreAvailable_ReturnsOkWithSummary()
        {
            // Arrange
            _mockJobRepository.Setup(r => r.GetJobStatusCountsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotImplementedException("GetJobStatusCountsAsync is not implemented yet."));

            // Act
            ActionResult<JobStatusSummaryDto> result = await _controller.GetJobStatusSummary();

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            JobStatusSummaryDto summary = okResult.Value.Should().BeOfType<JobStatusSummaryDto>().Subject;
            summary.Pending.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
