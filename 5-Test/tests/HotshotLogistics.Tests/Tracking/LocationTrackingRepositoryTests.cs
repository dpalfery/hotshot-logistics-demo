// <copyright file="LocationTrackingRepositoryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HotshotLogistics.Tests.Tracking
{
    /// <summary>
    ///     Integration tests for LocationTrackingRepository.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class LocationTrackingRepositoryTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
    {
        private readonly TestRowCleanup _cleanup = new();
        private readonly LocationTrackingRepository _locationTrackingRepository;

        // Unique per test instance: every row this class writes uses these ids, and every query is scoped to them,
        // so seed rows and rows from other classes or processes never change the outcome.
        private readonly string _jobId = $"test-loc-{Guid.NewGuid():N}";
        private readonly string _otherJobId = $"test-loc-{Guid.NewGuid():N}";
        private readonly int _driverId = Random.Shared.Next(900_000_000, 1_400_000_000);
        private readonly int _otherDriverId = Random.Shared.Next(1_400_000_001, int.MaxValue);

        /// <summary>
        ///     Initializes a new instance of the <see cref="LocationTrackingRepositoryTests" /> class.
        /// </summary>
        public LocationTrackingRepositoryTests(DatabaseTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            if (!TestDatabaseHelper.IsConfigured)
            {
                _locationTrackingRepository = null!;
                return;
            }

            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = TestDatabaseHelper.GetConnectionString()
                });

            IConfiguration configuration = configBuilder.Build();
            _locationTrackingRepository = new LocationTrackingRepository(configuration);
        }

        /// <inheritdoc />
        public ValueTask InitializeAsync()
        {
            _cleanup.TrackLocationJob(_jobId);
            _cleanup.TrackLocationJob(_otherJobId);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        ///     Deletes the rows this test wrote and fails the test when any remain.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _cleanup.CleanUpAsync();
        }

        /// <summary>
        ///     Tests that AddAsync creates a new location tracking record successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task AddAsync_CreatesLocationTrackingRecordSuccessfully()
        {
            // Arrange
            LocationTracking locationTracking = CreateTestLocationTracking();

            // Act
            LocationTracking result = await _locationTrackingRepository.AddAsync(locationTracking);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.JobId.Should().Be(locationTracking.JobId);
            result.DriverId.Should().Be(locationTracking.DriverId);
            result.Latitude.Should().Be(locationTracking.Latitude);
            result.Longitude.Should().Be(locationTracking.Longitude);
        }

        /// <summary>
        ///     Tests that GetByIdAsync retrieves a location tracking record successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByIdAsync_RetrievesLocationTrackingRecordSuccessfully()
        {
            // Arrange
            LocationTracking locationTracking = await CreateAndSaveTestLocationTrackingAsync();

            // Act
            LocationTracking? result = await _locationTrackingRepository.GetByIdAsync(locationTracking.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(locationTracking.Id);
            result.JobId.Should().Be(locationTracking.JobId);
            result.DriverId.Should().Be(locationTracking.DriverId);
            result.Latitude.Should().Be(locationTracking.Latitude);
            result.Longitude.Should().Be(locationTracking.Longitude);
        }

        /// <summary>
        ///     Tests that GetByJobIdAsync returns location tracking records for the correct job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByJobIdAsync_ReturnsLocationTrackingRecordsForCorrectJob()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            List<LocationTracking> result = (await _locationTrackingRepository.GetByJobIdAsync(_jobId)).ToList();

            // Assert: the three records of this job, oldest first
            result.Should().HaveCount(3);
            result.Should().OnlyContain(lt => lt.JobId == _jobId);
            result.Should().BeInAscendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        ///     Tests that GetByDriverIdAsync returns location tracking records for the correct driver.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByDriverIdAsync_ReturnsLocationTrackingRecordsForCorrectDriver()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            List<LocationTracking> result =
                (await _locationTrackingRepository.GetByDriverIdAsync(_driverId)).ToList();

            // Assert
            result.Should().HaveCount(3);
            result.Should().OnlyContain(lt => lt.DriverId == _driverId);
            result.Should().BeInDescendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        ///     Tests that GetByJobIdAndTimeRangeAsync returns location tracking records within the time range.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByJobIdAndTimeRangeAsync_ReturnsLocationTrackingRecordsWithinTimeRange()
        {
            // Arrange: records are at -60, -45 and -30 minutes
            await CreateTestLocationTrackingRecordsAsync();
            DateTime startTime = DateTime.UtcNow.AddMinutes(-50);
            DateTime endTime = DateTime.UtcNow.AddHours(2);

            // Act
            List<LocationTracking> result =
                (await _locationTrackingRepository.GetByJobIdAndTimeRangeAsync(_jobId, startTime, endTime)).ToList();

            // Assert: the -45 and -30 minute records only
            result.Should().HaveCount(2);
            result.Should().OnlyContain(lt =>
                lt.JobId == _jobId &&
                lt.Timestamp >= startTime &&
                lt.Timestamp <= endTime);
            result.Should().BeInAscendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        ///     Tests that GetByDriverIdAndTimeRangeAsync returns location tracking records within the time range.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByDriverIdAndTimeRangeAsync_ReturnsLocationTrackingRecordsWithinTimeRange()
        {
            // Arrange: records are at -60, -45 and -30 minutes
            await CreateTestLocationTrackingRecordsAsync();
            DateTime startTime = DateTime.UtcNow.AddMinutes(-50);
            DateTime endTime = DateTime.UtcNow.AddHours(2);

            // Act
            List<LocationTracking> result =
                (await _locationTrackingRepository.GetByDriverIdAndTimeRangeAsync(_driverId, startTime, endTime))
                .ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(lt =>
                lt.DriverId == _driverId &&
                lt.Timestamp >= startTime &&
                lt.Timestamp <= endTime);
            result.Should().BeInAscendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        ///     Tests that GetLatestByJobIdAsync returns the latest record for the job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetLatestByJobIdAsync_ReturnsLatestLocationTrackingRecordForJob()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            LocationTracking? result = await _locationTrackingRepository.GetLatestByJobIdAsync(_jobId);

            // Assert: the -30 minute record is the newest of the job
            result.Should().NotBeNull();
            result.JobId.Should().Be(_jobId);
            result.Longitude.Should().Be(-74.0064m);
        }

        /// <summary>
        ///     Tests that GetLatestByDriverIdAsync returns the latest record for the driver.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetLatestByDriverIdAsync_ReturnsLatestLocationTrackingRecordForDriver()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            LocationTracking? result = await _locationTrackingRepository.GetLatestByDriverIdAsync(_driverId);

            // Assert
            result.Should().NotBeNull();
            result.DriverId.Should().Be(_driverId);
            result.Longitude.Should().Be(-74.0064m);
        }

        /// <summary>
        ///     Tests that GetLatestByJobIdAsync with a count returns that many of the latest records.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetLatestByJobIdAsync_WithCount_ReturnsSpecifiedNumberOfLatestRecords()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();
            int count = 2;

            // Act
            IEnumerable<LocationTracking> rawResult =
                await _locationTrackingRepository.GetLatestByJobIdAsync(_jobId, count);
            List<LocationTracking> result = rawResult.ToList();

            // Assert
            result.Should().HaveCount(count);
            result.Should().OnlyContain(lt => lt.JobId == _jobId);
            result.Should().BeInDescendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        ///     Tests that GetByGeographicAreaAsync returns records within the area and leaves out distant ones.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByGeographicAreaAsync_ReturnsLocationTrackingRecordsWithinGeographicArea()
        {
            // Arrange: this job's records are in New York City, the other job's in Los Angeles
            await CreateTestLocationTrackingRecordsAsync();
            decimal centerLatitude = 40.7128m; // New York City
            decimal centerLongitude = -74.0060m;
            double radiusMiles = 50.0;

            // Act
            List<LocationTracking> result = (await _locationTrackingRepository.GetByGeographicAreaAsync(
                centerLatitude, centerLongitude, radiusMiles)).ToList();

            // Assert
            result.Should().BeInDescendingOrder(lt => lt.Timestamp);
            result.Count(lt => lt.JobId == _jobId).Should().Be(3);
            result.Should().NotContain(lt => lt.JobId == _otherJobId);
        }

        /// <summary>
        ///     Tests that DeleteOlderThanAsync deletes records older than the cutoff and keeps newer ones.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task DeleteOlderThanAsync_DeletesLocationTrackingRecordsOlderThanCutoffDate()
        {
            // Arrange: the cutoff is a day after year 1900, so only rows this suite backdates that far are
            // deleted; seed rows and rows other tests keep are never older than that.
            DateTime ancient = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            LocationTracking old = await _locationTrackingRepository.AddAsync(CreateTestLocationTracking(timestamp: ancient));
            LocationTracking recent = await _locationTrackingRepository.AddAsync(CreateTestLocationTracking());
            DateTime cutoffDate = ancient.AddDays(1);

            // Act
            int result = await _locationTrackingRepository.DeleteOlderThanAsync(cutoffDate);

            // Assert: another process may have deleted the old row first, so the count is only a lower bound
            result.Should().BeGreaterThanOrEqualTo(0);
            (await _locationTrackingRepository.ExistsAsync(old.Id)).Should().BeFalse();
            (await _locationTrackingRepository.ExistsAsync(recent.Id)).Should().BeTrue();
        }

        /// <summary>
        ///     Tests that GetTotalDistanceByJobIdAsync returns the distance of the job's records.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetTotalDistanceByJobIdAsync_ReturnsTotalDistanceForJob()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            double result = await _locationTrackingRepository.GetTotalDistanceByJobIdAsync(_jobId);

            // Assert: three points a few hundred metres apart; the other job's records add nothing
            result.Should().BeGreaterThan(0);
            result.Should().BeLessThan(5);
        }

        /// <summary>
        ///     Tests that GetTotalDistanceByDriverIdAsync returns the distance within the time range.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetTotalDistanceByDriverIdAsync_ReturnsTotalDistanceForDriverWithinTimeRange()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();
            DateTime startTime = DateTime.UtcNow.AddDays(-1);
            DateTime endTime = DateTime.UtcNow.AddDays(1);

            // Act
            double result =
                await _locationTrackingRepository.GetTotalDistanceByDriverIdAsync(_driverId, startTime, endTime);

            // Assert
            result.Should().BeGreaterThan(0);
            result.Should().BeLessThan(5);
        }

        /// <summary>
        ///     Tests that ExistsAsync returns true for an existing record.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task ExistsAsync_ReturnsTrueForExistingLocationTrackingRecord()
        {
            // Arrange
            LocationTracking locationTracking = await CreateAndSaveTestLocationTrackingAsync();

            // Act
            bool result = await _locationTrackingRepository.ExistsAsync(locationTracking.Id);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        ///     Tests that ExistsAsync returns false for a record that does not exist.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task ExistsAsync_ReturnsFalseForNonExistingLocationTrackingRecord()
        {
            // Arrange: identity values never get near the top of the range
            long nonExistingId = long.MaxValue;

            // Act
            bool result = await _locationTrackingRepository.ExistsAsync(nonExistingId);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        ///     Tests that GetCountByJobIdAsync counts the records of the job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetCountByJobIdAsync_ReturnsCorrectCountOfLocationTrackingRecordsForJob()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            int result = await _locationTrackingRepository.GetCountByJobIdAsync(_jobId);

            // Assert
            result.Should().Be(3);
        }

        /// <summary>
        ///     Tests that AddBatchAsync adds multiple records.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task AddBatchAsync_AddsMultipleLocationTrackingRecordsSuccessfully()
        {
            // Arrange
            List<LocationTracking> locationTrackingRecords =
            [
                CreateTestLocationTracking(),
                CreateTestLocationTracking(latitude: 40.7130m, longitude: -74.0062m),
                CreateTestLocationTracking(latitude: 40.7132m, longitude: -74.0064m)
            ];

            // Act
            int result = await _locationTrackingRepository.AddBatchAsync(locationTrackingRecords);

            // Assert
            result.Should().Be(locationTrackingRecords.Count);

            // Verify records were added
            List<LocationTracking> addedRecords =
                (await _locationTrackingRepository.GetByJobIdAsync(_jobId)).ToList();
            addedRecords.Should().HaveCount(locationTrackingRecords.Count);
        }

        private LocationTracking CreateTestLocationTracking(
            string? jobId = null,
            int? driverId = null,
            decimal latitude = 40.7128m,
            decimal longitude = -74.0060m,
            DateTime? timestamp = null)
        {
            return new LocationTracking
            {
                JobId = jobId ?? _jobId,
                DriverId = driverId ?? _driverId,
                Latitude = latitude,
                Longitude = longitude,
                Speed = 35.5m,
                Heading = 180,
                Accuracy = 5.0m,
                Timestamp = timestamp ?? DateTime.UtcNow
            };
        }

        private async Task<LocationTracking> CreateAndSaveTestLocationTrackingAsync()
        {
            LocationTracking locationTracking = CreateTestLocationTracking();
            return await _locationTrackingRepository.AddAsync(locationTracking);
        }

        private async Task CreateTestLocationTrackingRecordsAsync()
        {
            // Three records for this test's job and driver in New York City, two for the other job in Los Angeles
            var testData = new[]
            {
                new { JobId = _jobId, DriverId = _driverId, Lat = 40.7128m, Lon = -74.0060m, TimeOffset = -60 },
                new { JobId = _jobId, DriverId = _driverId, Lat = 40.7130m, Lon = -74.0062m, TimeOffset = -45 },
                new { JobId = _jobId, DriverId = _driverId, Lat = 40.7132m, Lon = -74.0064m, TimeOffset = -30 },
                new { JobId = _otherJobId, DriverId = _otherDriverId, Lat = 34.0522m, Lon = -118.2437m, TimeOffset = -15 },
                new { JobId = _otherJobId, DriverId = _otherDriverId, Lat = 34.0524m, Lon = -118.2439m, TimeOffset = 0 }
            };

            foreach (var data in testData)
            {
                LocationTracking locationTracking = CreateTestLocationTracking(
                    data.JobId,
                    data.DriverId,
                    data.Lat,
                    data.Lon,
                    DateTime.UtcNow.AddMinutes(data.TimeOffset));
                locationTracking.Speed = 30.0m + (decimal)(Random.Shared.NextDouble() * 20); // 30-50 mph
                locationTracking.Heading = Random.Shared.Next(0, 360);
                locationTracking.Accuracy = 3.0m + (decimal)(Random.Shared.NextDouble() * 7); // 3-10 m

                await _locationTrackingRepository.AddAsync(locationTracking);
            }
        }
    }
}
