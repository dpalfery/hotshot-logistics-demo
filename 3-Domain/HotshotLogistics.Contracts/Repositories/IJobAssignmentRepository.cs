using HotshotLogistics.Domain.DTOs;

namespace HotshotLogistics.Contracts.Repositories
{
    /// <summary>
    ///     Repository interface for JobAssignment operations.
    /// </summary>
    public interface IJobAssignmentRepository : IRepository<JobAssignmentDto>
    {
        /// <summary>
        ///     Gets job assignments by driver ID.
        /// </summary>
        /// <param name="driverId">The driver ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of job assignments for the driver.</returns>
        Task<IEnumerable<JobAssignmentDto>> GetByDriverIdAsync(int driverId,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets job assignments by job ID.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of job assignments for the job.</returns>
        Task<IEnumerable<JobAssignmentDto>>
            GetByJobIdAsync(string jobId, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets active job assignments.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of active job assignments.</returns>
        Task<IEnumerable<JobAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Creates a new job assignment.
        /// </summary>
        /// <param name="jobAssignment">The job assignment to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created job assignment.</returns>
        Task<JobAssignmentDto> CreateAsync(JobAssignmentDto jobAssignment,
            CancellationToken cancellationToken = default);
    }
}
