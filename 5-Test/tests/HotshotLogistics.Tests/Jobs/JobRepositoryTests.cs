// <copyright file="JobRepositoryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HotshotLogistics.Tests.Jobs
{
    /// <summary>
    ///     Integration tests for JobRepository.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class JobRepositoryTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
    {
        private readonly TestRowCleanup _cleanup = new();
        private readonly int _driverId = Random.Shared.Next(900_000_000, int.MaxValue);
        private readonly JobRepository _jobRepository;

        // Every row this class creates carries this unique customer id, and every query below is scoped to it,
        // so seed rows and rows from other classes or processes never change the outcome.
        private readonly string _customerId = $"test-{Guid.NewGuid():N}";

        /// <summary>
        ///     Initializes a new instance of the <see cref="JobRepositoryTests" /> class.
        /// </summary>
        public JobRepositoryTests(DatabaseTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            if (!TestDatabaseHelper.IsConfigured)
            {
                _jobRepository = null!;
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

        /// <inheritdoc />
        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        ///     Deletes the created jobs and fails the test when any remain.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _cleanup.CleanUpAsync();
        }

        /// <summary>
        ///     Tests that GetJobsAsync with filtering returns correct results.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsAsync_WithFiltering_ReturnsCorrectResults()
        {
            // Arrange
            await CreateTestJobsAsync();
            JobFilterDto filter = new()
            {
                CustomerId = _customerId,
                Status = JobStatus.Pending,
                Priority = JobPriority.High
            };
            PaginationParameters pagination = new() { PageNumber = 1, PageSize = 10 };

            // Act
            PagedResult<Job> result = await _jobRepository.GetJobsAsync(filter, pagination);

            // Assert: the created jobs 1 and 3 are the pending, high priority ones
            result.Should().NotBeNull();
            result.Items.Should().OnlyContain(j => j.Status == JobStatus.Pending && j.Priority == JobPriority.High);
            result.Items.Select(j => j.Title).Should().BeEquivalentTo(Titles(1, 3));
            result.TotalCount.Should().Be(2);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        /// <summary>
        ///     Tests that GetJobsAsync with pagination returns correct page.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            await CreateTestJobsAsync();
            JobFilterDto filter = new() { CustomerId = _customerId };
            PaginationParameters pagination = new() { PageNumber = 1, PageSize = 2 };
            SortParameters sort = new() { SortBy = "CreatedAt", SortDirection = SortDirection.Descending };

            // Act
            PagedResult<Job> result = await _jobRepository.GetJobsAsync(filter, pagination, sort);

            // Assert: three created jobs, two per page, newest first
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.Items.Should().OnlyContain(j => j.CustomerId == _customerId);
            result.Items.Should().BeInDescendingOrder(j => j.CreatedAt);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.TotalCount.Should().Be(3);
        }

        /// <summary>
        ///     Tests that GetJobsAsync with sorting returns correctly ordered results.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsAsync_WithSorting_ReturnsOrderedResults()
        {
            // Arrange
            await CreateTestJobsAsync();
            JobFilterDto filter = new() { CustomerId = _customerId };
            SortParameters sort = new() { SortBy = "Amount", SortDirection = SortDirection.Ascending };
            PaginationParameters pagination = new() { PageNumber = 1, PageSize = 10 };

            // Act
            PagedResult<Job> result = await _jobRepository.GetJobsAsync(filter, pagination, sort);

            // Assert
            result.Should().NotBeNull();
            result.Items.Select(j => j.Pricing.TotalAmount).Should().Equal(150.00m, 250.00m, 350.00m);
        }

        /// <summary>
        ///     Tests that GetJobsAsync with search term returns matching results.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsAsync_WithSearchTerm_ReturnsMatchingResults()
        {
            // Arrange
            await CreateTestJobsAsync();
            JobFilterDto filter = new() { SearchTerm = _customerId };
            PaginationParameters pagination = new() { PageNumber = 1, PageSize = 10 };

            // Act: the unique id is in each created job's title, so only they match
            PagedResult<Job> result = await _jobRepository.GetJobsAsync(filter, pagination);

            // Assert
            result.Should().NotBeNull();
            result.Items.Select(j => j.Title).Should().BeEquivalentTo(Titles(1, 2, 3));
        }

        /// <summary>
        ///     Tests that GetJobsAsync with date range filter returns correct results.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsAsync_WithDateRangeFilter_ReturnsCorrectResults()
        {
            // Arrange
            await CreateTestJobsAsync();
            JobFilterDto filter = new()
            {
                CustomerId = _customerId,
                CreatedAfter = DateTime.UtcNow.AddDays(-1),
                CreatedBefore = DateTime.UtcNow.AddDays(1)
            };
            PaginationParameters pagination = new() { PageNumber = 1, PageSize = 10 };

            // Act
            PagedResult<Job> result = await _jobRepository.GetJobsAsync(filter, pagination);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(3);
            result.Items.Should().OnlyContain(j =>
                j.CreatedAt >= filter.CreatedAfter &&
                j.CreatedAt <= filter.CreatedBefore);
        }

        /// <summary>
        ///     Tests that GetJobsByStatusAsync returns jobs with correct status.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsByStatusAsync_ReturnsJobsWithCorrectStatus()
        {
            // Arrange
            await CreateTestJobsAsync();
            JobStatus status = JobStatus.Pending;

            // Act
            List<Job> result = (await _jobRepository.GetJobsByStatusAsync(status)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(j => j.Status == status);
            result.Where(j => j.CustomerId == _customerId).Select(j => j.Title)
                .Should().BeEquivalentTo(Titles(1, 3));
        }

        /// <summary>
        ///     Tests that GetJobsByDriverAsync returns jobs for correct driver.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsByDriverAsync_ReturnsJobsForCorrectDriver()
        {
            // Arrange
            await CreateTestJobsAsync();
            int driverId = _driverId;

            // Act
            List<Job> result = (await _jobRepository.GetJobsByDriverAsync(driverId)).ToList();

            // Assert: only created job 1 is assigned to this class's unique driver id
            result.Should().NotBeNull();
            result.Should().OnlyContain(j => j.AssignedDriverId == driverId);
            result.Select(j => j.Title).Should().Equal(Titles(1));
        }

        /// <summary>
        ///     Tests that GetJobsByCustomerAsync returns jobs for correct customer.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsByCustomerAsync_ReturnsJobsForCorrectCustomer()
        {
            // Arrange
            await CreateTestJobsAsync();
            string customerId = _customerId;

            // Act
            List<Job> result = (await _jobRepository.GetJobsByCustomerAsync(customerId)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(j => j.CustomerId == customerId);
            result.Select(j => j.Title).Should().BeEquivalentTo(Titles(1, 2, 3));
        }

        /// <summary>
        ///     Tests that GetOverdueJobsAsync returns only overdue jobs.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetOverdueJobsAsync_ReturnsOnlyOverdueJobs()
        {
            // Arrange
            await CreateOverdueTestJobAsync();

            // Act
            List<Job> result = (await _jobRepository.GetOverdueJobsAsync()).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(j =>
                j.EstimatedDeliveryTime < DateTime.UtcNow &&
                j.Status != JobStatus.Received &&
                j.Status != JobStatus.Pending);
            result.Should().ContainSingle(j => j.CustomerId == _customerId && j.Title == $"Overdue {_customerId}");
        }

        /// <summary>
        ///     Tests that GetJobCountAsync returns correct count.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            await CreateTestJobsAsync();
            JobFilterDto filter = new() { CustomerId = _customerId, Status = JobStatus.Pending };

            // Act
            int count = await _jobRepository.GetJobCountAsync(filter);

            // Assert: created jobs 1 and 3 are pending
            count.Should().Be(2);
        }

        /// <summary>
        ///     Tests that filtering by multiple statuses works correctly.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsAsync_WithStatusFilter_ReturnsCorrectResults()
        {
            // Arrange
            await CreateTestJobsAsync();
            JobFilterDto filter = new()
            {
                CustomerId = _customerId,
                StatusList = [JobStatus.Pending, JobStatus.Assigned]
            };
            PaginationParameters pagination = new() { PageNumber = 1, PageSize = 10 };

            // Act
            PagedResult<Job> result = await _jobRepository.GetJobsAsync(filter, pagination);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(3);
            result.Items.Should().OnlyContain(j =>
                j.Status == JobStatus.Pending || j.Status == JobStatus.Assigned);
        }

        /// <summary>
        ///     Tests that filtering by amount range works correctly.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetJobsAsync_WithAmountRange_ReturnsCorrectResults()
        {
            // Arrange
            await CreateTestJobsAsync();
            JobFilterDto filter = new()
            {
                CustomerId = _customerId,
                MinAmount = 100m,
                MaxAmount = 500m
            };
            PaginationParameters pagination = new() { PageNumber = 1, PageSize = 10 };

            // Act
            PagedResult<Job> result = await _jobRepository.GetJobsAsync(filter, pagination);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(3);
            result.Items.Should().OnlyContain(j =>
                j.Pricing.TotalAmount >= 100m && j.Pricing.TotalAmount <= 500m);
        }

        private string Title(int number)
        {
            return $"Test Job {number} {_customerId}";
        }

        private string[] Titles(params int[] numbers)
        {
            return numbers.Select(Title).ToArray();
        }

        /// <summary>
        ///     Creates test jobs for testing purposes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CreateTestJobsAsync()
        {
            List<Job> jobs =
            [
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = _customerId,
                    Title = Title(1),
                    PickupLocation = new Location { Address = "123 Test St" },
                    DeliveryLocation = new Location { Address = "456 Test Ave" },
                    Status = JobStatus.Pending,
                    Priority = JobPriority.High,
                    Amount = 250.00m,
                    EstimatedDeliveryTime = DateTime.UtcNow.AddHours(4),
                    ScheduledPickupTime = DateTime.UtcNow.AddHours(1),
                    AssignedDriverId = _driverId,
                    SpecialInstructions = "Handle with care",
                    CreatedAt = DateTime.UtcNow,
                    Pricing = new PricingDetails { TotalAmount = 250.00m }
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = _customerId,
                    Title = Title(2),
                    PickupLocation = new Location { Address = "789 Test Blvd" },
                    DeliveryLocation = new Location { Address = "321 Test Rd" },
                    Status = JobStatus.Assigned,
                    Priority = JobPriority.Medium,
                    Amount = 150.00m,
                    EstimatedDeliveryTime = DateTime.UtcNow.AddHours(6),
                    ScheduledPickupTime = DateTime.UtcNow.AddHours(2),
                    AssignedDriverId = null,
                    SpecialInstructions = "Fragile items",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                    Pricing = new PricingDetails { TotalAmount = 150.00m }
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = _customerId,
                    Title = Title(3),
                    PickupLocation = new Location { Address = "555 Test Way" },
                    DeliveryLocation = new Location { Address = "777 Test Ln" },
                    Status = JobStatus.Pending,
                    Priority = JobPriority.High,
                    Amount = 350.00m,
                    EstimatedDeliveryTime = DateTime.UtcNow.AddHours(8),
                    ScheduledPickupTime = DateTime.UtcNow.AddHours(3),
                    AssignedDriverId = null,
                    SpecialInstructions = "Rush delivery",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-60),
                    Pricing = new PricingDetails { TotalAmount = 350.00m }
                }
            ];

            foreach (Job job in jobs)
            {
                _cleanup.TrackJob(job.Id);
                await _jobRepository.CreateJobAsync(job);
            }
        }

        /// <summary>
        ///     Creates an overdue test job for testing purposes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CreateOverdueTestJobAsync()
        {
            Job job = new()
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = _customerId,
                Title = $"Overdue {_customerId}",
                PickupLocation = new Location { Address = "999 Overdue St" },
                DeliveryLocation = new Location { Address = "888 Late Ave" },
                Status = JobStatus.EnRoute,
                Priority = JobPriority.High,
                Amount = 400.00m,
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(-2),
                ScheduledPickupTime = DateTime.UtcNow.AddHours(-4),
                AssignedDriverId = null,
                SpecialInstructions = "Overdue delivery",
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                Pricing = new PricingDetails { TotalAmount = 400.00m }
            };

            _cleanup.TrackJob(job.Id);
            await _jobRepository.CreateJobAsync(job);
        }
    }
}
