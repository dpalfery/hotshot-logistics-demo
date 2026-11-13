using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Contracts.Repositories
{
    public interface IJobRepository
    {
        /// <summary>
        ///     Gets all jobs.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of all jobs.</returns>
        Task<IEnumerable<Job>> GetJobsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets a job by its identifier.
        /// </summary>
        /// <param name="id">The job identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The job if found, null otherwise.</returns>
        Task<Job?> GetJobByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Creates a new job.
        /// </summary>
        /// <param name="job">The job to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created job.</returns>
        Task<Job> CreateJobAsync(Job job, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Updates an existing job.
        /// </summary>
        /// <param name="id">The job identifier.</param>
        /// <param name="jobDetails">The updated job details.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated job if found, null otherwise.</returns>
        Task<Job?> UpdateJobAsync(string id, Job jobDetails, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Deletes a job by its identifier.
        /// </summary>
        /// <param name="id">The job identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the job was deleted, false otherwise.</returns>
        Task<bool> DeleteJobAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets jobs with filtering and pagination support.
        /// </summary>
        /// <param name="filter">The filter criteria.</param>
        /// <param name="pagination">The pagination parameters.</param>
        /// <param name="sort">The sort parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paged result of jobs.</returns>
        Task<PagedResult<Job>> GetJobsAsync(
            JobFilterDto? filter = null,
            PaginationParameters? pagination = null,
            SortParameters? sort = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets the number of jobs for each status.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A dictionary keyed by job status with the count for each status.</returns>
        Task<IReadOnlyDictionary<JobStatus, int>> GetJobStatusCountsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets jobs by status.
        /// </summary>
        /// <param name="status">The job status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of jobs with the specified status.</returns>
        Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets jobs assigned to a specific driver.
        /// </summary>
        /// <param name="driverId">The driver ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of jobs assigned to the driver.</returns>
        Task<IEnumerable<Job>> GetJobsByDriverAsync(int driverId, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets jobs for a specific customer.
        /// </summary>
        /// <param name="customerId">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of jobs for the customer.</returns>
        Task<IEnumerable<Job>> GetJobsByCustomerAsync(string customerId, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets overdue jobs (past estimated delivery time).
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of overdue jobs.</returns>
        Task<IEnumerable<Job>> GetOverdueJobsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets the total count of jobs matching the filter criteria.
        /// </summary>
        /// <param name="filter">The filter criteria.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The total count of matching jobs.</returns>
        Task<int> GetJobCountAsync(JobFilterDto? filter = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets jobs assigned to a specific driver by driver ID.
        /// </summary>
        /// <param name="driverId">The driver ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of jobs assigned to the driver.</returns>
        Task<IEnumerable<Job>> GetByDriverIdAsync(int driverId, CancellationToken cancellationToken = default);
    }
}
