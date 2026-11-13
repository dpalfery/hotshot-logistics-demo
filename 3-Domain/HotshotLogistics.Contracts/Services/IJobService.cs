using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Contracts.Services
{
    public interface IJobService
    {
        /// <summary>
        ///     Gets a summary of job counts by status.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The job status summary.</returns>
        Task<JobStatusSummaryDto> GetJobStatusSummaryAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<Job>> GetJobsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets jobs with filtering, pagination, and sorting support.
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

        Task<Job?> GetJobByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Job> CreateJobAsync(Job job, CancellationToken cancellationToken = default);
        Task<Job?> UpdateJobAsync(string id, Job jobDetails, CancellationToken cancellationToken = default);
        Task<bool> DeleteJobAsync(string id, CancellationToken cancellationToken = default);

        // Job lifecycle management methods
        Task<Job> AssignDriverAsync(string jobId, int driverId, CancellationToken cancellationToken = default);
        Task<Job> UpdateJobStatusAsync(string jobId, JobStatus status, CancellationToken cancellationToken = default);
        Task<bool> ValidateJobAsync(Job job, CancellationToken cancellationToken = default);

        Task<bool> IsDriverAvailableAsync(int driverId, DateTime startTime, DateTime? endTime = null,
            CancellationToken cancellationToken = default);
    }
}
