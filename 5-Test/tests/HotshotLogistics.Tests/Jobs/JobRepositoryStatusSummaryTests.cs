// <copyright file="JobRepositoryStatusSummaryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HotshotLogistics.Tests.Jobs
{
    /// <summary>
    ///     Database tests for the job status summary repository method.
    /// </summary>
    public class JobRepositoryStatusSummaryTests : IClassFixture<DatabaseTestFixture>, IDisposable
    {
        private readonly List<string> _createdJobIds = new();
        private readonly JobRepository? _jobRepository;

        /// <summary>
        ///     Initializes a new instance of the <see cref="JobRepositoryStatusSummaryTests" /> class.
        /// </summary>
        /// <param name="fixture">The database test fixture.</param>
        public JobRepositoryStatusSummaryTests(DatabaseTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            if (!TestDatabaseHelper.IsConfigured)
            {
                _jobRepository = null;
                return;
            }

            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = TestDatabaseHelper.GetConnectionString()
                });

            IConfiguration configuration = configBuilder.Build();
            _jobRepository = new JobRepository(configuration);
        }

        /// <summary>
        ///     Cleans up created jobs after each test.
        /// </summary>
        public void Dispose()
        {
            if (_jobRepository == null)
            {
                return;
            }

            foreach (string jobId in _createdJobIds)
            {
                try
                {
                    _jobRepository.DeleteJobAsync(jobId).GetAwaiter().GetResult();
                }
                catch
                {
                    // Ignore cleanup errors.
                }
            }
        }

        /// <summary>
        ///     Tests the repository returns counts grouped by the status values.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [DatabaseFact]
        public async Task GetJobStatusCountsAsync_WhenJobsExist_ReturnsCountsByStatus()
        {
            if (_jobRepository == null)
            {
                return;
            }

            Job pendingJob = CreateTestJob("status-summary-pending", JobStatus.Pending);
            Job assignedJob = CreateTestJob("status-summary-assigned", JobStatus.Assigned);
            Job enRouteJob = CreateTestJob("status-summary-enroute", JobStatus.EnRoute);
            Job receivedJob = CreateTestJob("status-summary-received", JobStatus.Received);

            await _jobRepository.CreateJobAsync(pendingJob);
            await _jobRepository.CreateJobAsync(assignedJob);
            await _jobRepository.CreateJobAsync(enRouteJob);
            await _jobRepository.CreateJobAsync(receivedJob);

            _createdJobIds.Add(pendingJob.Id);
            _createdJobIds.Add(assignedJob.Id);
            _createdJobIds.Add(enRouteJob.Id);
            _createdJobIds.Add(receivedJob.Id);

            IReadOnlyDictionary<JobStatus, int> counts = await _jobRepository.GetJobStatusCountsAsync();

            counts.Should().ContainKey(JobStatus.Pending);
            counts.Should().ContainKey(JobStatus.Assigned);
            counts.Should().ContainKey(JobStatus.EnRoute);
            counts.Should().ContainKey(JobStatus.Received);
            counts[JobStatus.Pending].Should().BeGreaterThan(0);
            counts[JobStatus.Assigned].Should().BeGreaterThan(0);
            counts[JobStatus.EnRoute].Should().BeGreaterThan(0);
            counts[JobStatus.Received].Should().BeGreaterThan(0);
        }

        private static Job CreateTestJob(string id, JobStatus status)
        {
            return new Job
            {
                Id = id,
                Title = $"Summary Job {id}",
                CustomerId = "cust-summary",
                Status = status,
                Priority = JobPriority.Medium,
                Amount = 100.00m,
                CreatedAt = DateTime.UtcNow,
                ScheduledPickupTime = DateTime.UtcNow.AddHours(1),
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(4),
                PickupLocation = new Location { Address = "123 Pickup St", Latitude = 40.7128m, Longitude = -74.0060m },
                DeliveryLocation = new Location { Address = "456 Delivery Ave", Latitude = 40.7589m, Longitude = -73.9851m },
                Cargo = new CargoDetails { Description = "Summary cargo", Weight = 100, Value = 1000 },
                Pricing = new PricingDetails { BaseRate = 100, TotalAmount = 100 },
                Documents = new List<JobDocument>(),
                Tracking = new TrackingInfo { CurrentStatus = "Created", IsActive = false }
            };
        }
    }
}
