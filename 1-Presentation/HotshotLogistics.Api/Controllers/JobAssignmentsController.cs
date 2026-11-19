// <copyright file="JobAssignmentsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Application.Authorization;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    ///     API controller for managing job assignments.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class JobAssignmentsController : ApiControllerBase
    {
        private readonly IJobAssignmentService _assignmentService;
        private readonly ILogger<JobAssignmentsController> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="JobAssignmentsController" /> class.
        /// </summary>
        /// <param name="assignmentService">The job assignment service.</param>
        /// <param name="logger">The _logger.</param>
        public JobAssignmentsController(
            IJobAssignmentService assignmentService,
            ILogger<JobAssignmentsController> logger)
        {
            _assignmentService = assignmentService ?? throw new ArgumentNullException(nameof(assignmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Gets a job assignment by ID.
        /// </summary>
        /// <param name="id">The assignment ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The job assignment if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OwnResource)]
        [ProducesResponseType(typeof(JobAssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JobAssignmentDto>> GetById(string id,
            CancellationToken cancellationToken = default)
        {
            JobAssignmentDto? assignment = await _assignmentService.GetByIdAsync(id, cancellationToken);
            if (assignment == null)
            {
                return NotFound();
            }

            return Ok(assignment);
        }

        /// <summary>
        ///     Gets all job assignments.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of job assignments.</returns>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<JobAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobAssignmentDto>>> GetAll(
            CancellationToken cancellationToken = default)
        {
            IEnumerable<JobAssignmentDto> assignments = await _assignmentService.GetAllAsync(cancellationToken);
            return Ok(assignments);
        }

        /// <summary>
        ///     Gets job assignments by driver ID.
        /// </summary>
        /// <param name="driverId">The driver ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of job assignments for the driver.</returns>
        [HttpGet("driver/{driverId}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrDriver)]
        [ProducesResponseType(typeof(IEnumerable<JobAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobAssignmentDto>>> GetByDriverId(int driverId,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<JobAssignmentDto> assignments =
                await _assignmentService.GetByDriverIdAsync(driverId, cancellationToken);
            return Ok(assignments);
        }

        /// <summary>
        ///     Gets job assignments by job ID.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of job assignments for the job.</returns>
        [HttpGet("job/{jobId}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<JobAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobAssignmentDto>>> GetByJobId(string jobId,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<JobAssignmentDto> assignments =
                await _assignmentService.GetByJobIdAsync(jobId, cancellationToken);
            return Ok(assignments);
        }

        /// <summary>
        ///     Gets active job assignments.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of active job assignments.</returns>
        [HttpGet("active")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<JobAssignmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<JobAssignmentDto>>> GetActive(
            CancellationToken cancellationToken = default)
        {
            IEnumerable<JobAssignmentDto> assignments =
                await _assignmentService.GetActiveAssignmentsAsync(cancellationToken);
            return Ok(assignments);
        }

        /// <summary>
        ///     Assigns a job to a driver.
        /// </summary>
        /// <param name="request">The assignment request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created job assignment.</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(JobAssignmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<JobAssignmentDto>> AssignJob(
            [FromBody] AssignJobRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                JobAssignmentDto assignment = await _assignmentService.AssignJobAsync(
                    request.JobId,
                    request.DriverId,
                    cancellationToken);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = assignment.Id },
                    assignment);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Failed to assign job: {Message}", ex.Message);
                return NotFoundProblem(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to assign job: {Message}", ex.Message);
                return ConflictProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Updates a job assignment status.
        /// </summary>
        /// <param name="id">The assignment ID.</param>
        /// <param name="status">The new status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated job assignment.</returns>
        [HttpPut("{id}/status")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrDriver)]
        [ProducesResponseType(typeof(JobAssignmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JobAssignmentDto>> UpdateStatus(
            string id,
            [FromBody] JobAssignmentStatus status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                JobAssignmentDto assignment =
                    await _assignmentService.UpdateAssignmentStatusAsync(id, status, cancellationToken);
                return Ok(assignment);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Failed to update assignment status: {Message}", ex.Message);
                return NotFoundProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Unassigns a job from a driver.
        /// </summary>
        /// <param name="id">The assignment ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful; otherwise, 404 Not Found.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnassignJob(string id, CancellationToken cancellationToken = default)
        {
            bool result = await _assignmentService.UnassignJobAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }

    /// <summary>
    ///     Request model for assigning a job to a driver.
    /// </summary>
    public class AssignJobRequest
    {
        /// <summary>
        ///     Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the driver ID.
        /// </summary>
        public int DriverId { get; set; }
    }
}
