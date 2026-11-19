// <copyright file="LocationTrackingRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Contracts.Repositories
{
    /// <summary>
    ///     Interface for location tracking repository operations.
    /// </summary>
    public interface ILocationTrackingRepository : IRepository<LocationTracking>
    {
        /// <summary>
        ///     Gets all location tracking records for a specific job.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <returns>A collection of location tracking records for the job.</returns>
        Task<IEnumerable<LocationTracking>> GetByJobIdAsync(string jobId);

        /// <summary>
        ///     Gets all location tracking records for a specific driver.
        /// </summary>
        /// <param name="driverId">The driver identifier.</param>
        /// <returns>A collection of location tracking records for the driver.</returns>
        Task<IEnumerable<LocationTracking>> GetByDriverIdAsync(int driverId);

        /// <summary>
        ///     Gets location tracking records for a job within a specific time range.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <returns>A collection of location tracking records within the time range.</returns>
        Task<IEnumerable<LocationTracking>> GetByJobIdAndTimeRangeAsync(string jobId, DateTime startTime,
            DateTime endTime);

        /// <summary>
        ///     Gets location tracking records for a driver within a specific time range.
        /// </summary>
        /// <param name="driverId">The driver identifier.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <returns>A collection of location tracking records within the time range.</returns>
        Task<IEnumerable<LocationTracking>> GetByDriverIdAndTimeRangeAsync(int driverId, DateTime startTime,
            DateTime endTime);

        /// <summary>
        ///     Gets the latest location tracking record for a specific job.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <returns>The latest location tracking record for the job, or null if none found.</returns>
        Task<LocationTracking?> GetLatestByJobIdAsync(string jobId);

        /// <summary>
        ///     Gets the latest location tracking record for a specific driver.
        /// </summary>
        /// <param name="driverId">The driver identifier.</param>
        /// <returns>The latest location tracking record for the driver, or null if none found.</returns>
        Task<LocationTracking?> GetLatestByDriverIdAsync(int driverId);

        /// <summary>
        ///     Gets the latest location tracking records for a job up to a specified count.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="count">The maximum number of records to return.</param>
        /// <returns>The latest location tracking records for the job.</returns>
        Task<IEnumerable<LocationTracking>> GetLatestByJobIdAsync(string jobId, int count);

        /// <summary>
        ///     Gets location tracking records within a geographic area.
        /// </summary>
        /// <param name="centerLatitude">The center latitude of the search area.</param>
        /// <param name="centerLongitude">The center longitude of the search area.</param>
        /// <param name="radiusMiles">The radius in miles.</param>
        /// <param name="startTime">The start time for the search.</param>
        /// <param name="endTime">The end time for the search.</param>
        /// <returns>A collection of location tracking records within the geographic area.</returns>
        Task<IEnumerable<LocationTracking>> GetByGeographicAreaAsync(
            decimal centerLatitude,
            decimal centerLongitude,
            double radiusMiles,
            DateTime? startTime = null,
            DateTime? endTime = null);

        /// <summary>
        ///     Deletes location tracking records older than the specified date.
        /// </summary>
        /// <param name="cutoffDate">The cutoff date for deletion.</param>
        /// <returns>The number of records deleted.</returns>
        Task<int> DeleteOlderThanAsync(DateTime cutoffDate);

        /// <summary>
        ///     Gets the total distance traveled for a job based on location tracking records.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <returns>The total distance traveled in miles.</returns>
        Task<double> GetTotalDistanceByJobIdAsync(string jobId);

        /// <summary>
        ///     Gets the total distance traveled by a driver within a time range.
        /// </summary>
        /// <param name="driverId">The driver identifier.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <returns>The total distance traveled in miles.</returns>
        Task<double> GetTotalDistanceByDriverIdAsync(int driverId, DateTime startTime, DateTime endTime);

        /// <summary>
        ///     Gets the count of location tracking records for a job.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <returns>The count of location tracking records.</returns>
        Task<int> GetCountByJobIdAsync(string jobId);

        /// <summary>
        ///     Adds multiple location tracking records in a batch operation.
        /// </summary>
        /// <param name="locationTrackingRecords">The location tracking records to add.</param>
        /// <returns>The number of records added.</returns>
        Task<int> AddBatchAsync(IEnumerable<LocationTracking> locationTrackingRecords);
    }
}
