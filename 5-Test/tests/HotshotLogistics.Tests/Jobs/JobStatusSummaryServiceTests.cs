// <copyright file="JobStatusSummaryServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Jobs
{
    /// <summary>
    ///     Unit tests for the job status summary service behavior.
    /// </summary>
    public class JobStatusSummaryServiceTests
    {
        private readonly JobService _service;

        /// <summary>
        ///     Initializes a new instance of the <see cref="JobStatusSummaryServiceTests" /> class.
        /// </summary>
        public JobStatusSummaryServiceTests()
        {
            Mock<IJobRepository> mockJobRepository = new();
            Mock<ICustomerRepository> mockCustomerRepository = new();
            Mock<IDriverRepository> mockDriverRepository = new();
            Mock<INotificationService> mockNotificationService = new();
            Mock<IMappingService> mockMappingService = new();
            Mock<ILogger<JobService>> mockLogger = new();

            mockJobRepository.Setup(r => r.GetJobStatusCountsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<JobStatus, int>
                {
                    [JobStatus.Pending] = 3,
                    [JobStatus.Assigned] = 2,
                    [JobStatus.EnRoute] = 1,
                    [JobStatus.Received] = 4
                });

            _service = new JobService(
                mockJobRepository.Object,
                mockCustomerRepository.Object,
                mockDriverRepository.Object,
                mockNotificationService.Object,
                mockMappingService.Object,
                mockLogger.Object);
        }

        /// <summary>
        ///     Tests the service aggregates the repository job status counts into the summary DTO.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobStatusSummaryAsync_WhenCountsExist_ReturnsSummaryWithAllStatuses()
        {
            // Act
            JobStatusSummaryDto result = await _service.GetJobStatusSummaryAsync();

            // Assert
            result.Should().NotBeNull();
            result.Pending.Should().Be(3);
            result.Assigned.Should().Be(2);
            result.EnRoute.Should().Be(1);
            result.Received.Should().Be(4);
        }
    }
}
