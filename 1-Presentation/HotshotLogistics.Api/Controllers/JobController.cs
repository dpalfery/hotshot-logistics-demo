// <copyright file="JobController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation;
using FluentValidation.Results;
using HotshotLogistics.Application.Authorization;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValidationException = HotshotLogistics.Core.Exceptions.ValidationException;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    ///     API controller for managing jobs with CRUD operations.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JobController : ControllerBase
    {
        private readonly IJobRepository _jobRepository;
        private readonly IJobService _jobService;
        private readonly IValidator<Job> _jobValidator;
        private readonly ILogger<JobController> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="JobController" /> class.
        /// </summary>
        /// <param name="jobService">The job service.</param>
        /// <param name="jobRepository">The job repository.</param>
        /// <param name="logger">The _logger.</param>
        /// <param name="jobValidator">The job validator.</param>
        public JobController(
            IJobService jobService,
            IJobRepository jobRepository,
            ILogger<JobController> logger,
            IValidator<Job> jobValidator)
        {
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobValidator = jobValidator ?? throw new ArgumentNullException(nameof(jobValidator));
        }

        /// <summary>
        ///     Gets all jobs with optional filtering and pagination.
        /// </summary>
        /// <param name="status">Filter by job status.</param>
        /// <param name="priority">Filter by job priority.</param>
        /// <param name="customerId">Filter by customer ID.</param>
        /// <param name="assignedDriverId">Filter by assigned driver ID.</param>
        /// <param name="createdAfter">Filter by creation date (after).</param>
        /// <param name="createdBefore">Filter by creation date (before).</param>
        /// <param name="scheduledAfter">Filter by scheduled pickup time (after).</param>
        /// <param name="scheduledBefore">Filter by scheduled pickup time (before).</param>
        /// <param name="minAmount">Filter by minimum amount.</param>
        /// <param name="maxAmount">Filter by maximum amount.</param>
        /// <param name="searchTerm">Search term for title or address.</param>
        /// <param name="hasAssignedDriver">Filter by driver assignment status.</param>
        /// <param name="isOverdue">Filter for overdue jobs.</param>
        /// <param name="pageNumber">Page number (1-based).</param>
        /// <param name="pageSize">Page size (max 100).</param>
        /// <param name="sortBy">Field to sort by.</param>
        /// <param name="sortDirection">Sort direction (Ascending/Descending).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paged result of jobs.</returns>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(PagedResult<Job>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<Job>>> GetJobs(
            [FromQuery] JobStatus? status = null,
            [FromQuery] JobPriority? priority = null,
            [FromQuery] string? customerId = null,
            [FromQuery] int? assignedDriverId = null,
            [FromQuery] DateTime? createdAfter = null,
            [FromQuery] DateTime? createdBefore = null,
            [FromQuery] DateTime? scheduledAfter = null,
            [FromQuery] DateTime? scheduledBefore = null,
            [FromQuery] decimal? minAmount = null,
            [FromQuery] decimal? maxAmount = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? hasAssignedDriver = null,
            [FromQuery] bool? isOverdue = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] SortDirection sortDirection = SortDirection.Descending,
            CancellationToken cancellationToken = default)
        {
            JobFilterDto filter = new()
            {
                Status = status,
                Priority = priority,
                CustomerId = customerId,
                AssignedDriverId = assignedDriverId,
                CreatedAfter = createdAfter,
                CreatedBefore = createdBefore,
                ScheduledPickupAfter = scheduledAfter,
                ScheduledPickupBefore = scheduledBefore,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                SearchTerm = searchTerm,
                HasAssignedDriver = hasAssignedDriver,
                IsOverdue = isOverdue
            };

            PaginationParameters pagination = new()
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            SortParameters sort = new()
            {
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            _logger.LogInformation(
                "JobController.GetJobs called with filter: {@Filter}, pagination: {@Pagination}, sort: {@Sort}",
                filter, pagination, sort);

            PagedResult<Job> result = await _jobRepository.GetJobsAsync(filter, pagination, sort, cancellationToken);

            _logger.LogInformation("JobController.GetJobs returned {TotalCount} jobs out of {ItemCount} items",
                result.TotalCount, result.Items.Count());

            return Ok(result);
        }

        /// <summary>
        ///     Gets a summary of job counts by status.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A job status summary.</returns>
        [HttpGet("status-summary")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(JobStatusSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<JobStatusSummaryDto>> GetJobStatusSummary(
            CancellationToken cancellationToken = default)
        {
            try
            {
                JobStatusSummaryDto summary = await _jobService.GetJobStatusSummaryAsync(cancellationToken);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the job status summary.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Gets a job by ID.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The job if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OwnResource)]
        [ProducesResponseType(typeof(Job), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Job>> GetJobById(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                Job? job = await _jobService.GetJobByIdAsync(id, cancellationToken);
                if (job == null)
                {
                    return NotFound($"Job with ID {id} not found");
                }

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving job");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Creates a new job.
        /// </summary>
        /// <param name="jobDto">The job data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created job.</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(Job), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Job>> CreateJob(
            [FromBody] Job? jobDto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (jobDto == null)
                {
                    return BadRequest("Job data is required");
                }

                // Validate the job data
                ValidationResult validationResult = await _jobValidator.ValidateAsync(jobDto, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Job validation failed: {Errors}",
                        string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                    return BadRequest(new
                    {
                        Message = "Job validation failed",
                        Errors = validationResult.Errors.Select(e => new
                        {
                            Field = e.PropertyName,
                            Message = e.ErrorMessage
                        })
                    });
                }

                Job createdJob = await _jobService.CreateJobAsync(jobDto, cancellationToken);
                return CreatedAtAction(nameof(GetJobById), new { id = createdJob.Id }, createdJob);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid job data provided: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Referenced entity not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Job validation failed: {Message}", ex.Message);
                return BadRequest(new
                {
                    Message = "Job validation failed",
                    Errors = ex.Errors.SelectMany(kvp => kvp.Value.Select(error => new
                    {
                        Field = kvp.Key,
                        Message = error
                    }))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating job");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Updates an existing job.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <param name="jobDto">The updated job data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated job.</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(Job), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Job>> UpdateJob(
            string id,
            [FromBody] Job? jobDto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (jobDto == null)
                {
                    return BadRequest("Job data is required");
                }

                // Ensure the ID in the URL matches the job data
                jobDto.Id = id;

                // Validate the job data using FluentValidation
                ValidationResult validationResult = await _jobValidator.ValidateAsync(jobDto, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Job validation failed during update: {Errors}",
                        string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                    return BadRequest(new
                    {
                        Message = "Job validation failed",
                        Errors = validationResult.Errors.Select(e => new
                        {
                            Field = e.PropertyName,
                            Message = e.ErrorMessage
                        })
                    });
                }

                Job? updatedJob = await _jobService.UpdateJobAsync(id, jobDto, cancellationToken);
                if (updatedJob == null)
                {
                    return NotFound($"Job with ID {id} not found");
                }

                return Ok(updatedJob);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid job data provided: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Job validation failed during update: {Message}", ex.Message);
                return BadRequest(new
                {
                    Message = "Job validation failed",
                    Errors = ex.Errors.SelectMany(kvp => kvp.Value.Select(error => new
                    {
                        Field = kvp.Key,
                        Message = error
                    }))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating job");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Deletes a job.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful; otherwise, 404 Not Found or 400 Bad Request.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteJob(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                bool result = await _jobService.DeleteJobAsync(id, cancellationToken);
                if (!result)
                {
                    return NotFound($"Job with ID {id} not found");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete job: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting job");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Assigns a driver to a job.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <param name="request">The driver assignment request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated job with driver assignment.</returns>
        [HttpPost("{id}/assign-driver")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(Job), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Job>> AssignDriver(
            string id,
            [FromBody] AssignDriverRequest? request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Driver assignment request is required");
                }

                Job updatedJob = await _jobService.AssignDriverAsync(id, request.DriverId, cancellationToken);
                return Ok(updatedJob);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid assignment request: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot assign driver to job: {Message}", ex.Message);
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while assigning driver to job");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Updates the status of a job.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <param name="request">The status update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated job.</returns>
        [HttpPut("{id}/status")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrDriver)]
        [ProducesResponseType(typeof(Job), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Job>> UpdateJobStatus(
            string id,
            [FromBody] UpdateJobStatusRequest? request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Status update request is required");
                }

                Job updatedJob = await _jobService.UpdateJobStatusAsync(id, request.Status, cancellationToken);
                return Ok(updatedJob);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid status update request: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating job status");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Gets jobs by status.
        /// </summary>
        /// <param name="status">The job status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of jobs with the specified status.</returns>
        [HttpGet("by-status/{status}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Job>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Job>>> GetJobsByStatus(
            JobStatus status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                IEnumerable<Job> jobs = await _jobRepository.GetJobsByStatusAsync(status, cancellationToken);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving jobs by status");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Gets jobs assigned to a specific driver.
        /// </summary>
        /// <param name="driverId">The driver ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of jobs assigned to the driver.</returns>
        [HttpGet("by-driver/{driverId}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrDriver)]
        [ProducesResponseType(typeof(IEnumerable<Job>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Job>>> GetJobsByDriver(
            int driverId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                IEnumerable<Job> jobs = await _jobRepository.GetJobsByDriverAsync(driverId, cancellationToken);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving jobs for driver");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Gets jobs for a specific customer.
        /// </summary>
        /// <param name="customerId">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of jobs for the customer.</returns>
        [HttpGet("by-customer/{customerId}")]
        [Authorize(Policy = AuthorizationPolicies.CustomerResource)]
        [ProducesResponseType(typeof(IEnumerable<Job>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Job>>> GetJobsByCustomer(
            string customerId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                IEnumerable<Job> jobs = await _jobRepository.GetJobsByCustomerAsync(customerId, cancellationToken);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving jobs for customer");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        /// <summary>
        ///     Gets overdue jobs.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of overdue jobs.</returns>
        [HttpGet("overdue")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Job>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Job>>> GetOverdueJobs(CancellationToken cancellationToken = default)
        {
            try
            {
                IEnumerable<Job> jobs = await _jobRepository.GetOverdueJobsAsync(cancellationToken);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving overdue jobs");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }
    }

    /// <summary>
    ///     Request model for assigning a driver to a job.
    /// </summary>
    public class AssignDriverRequest
    {
        /// <summary>
        ///     Gets or sets the driver ID.
        /// </summary>
        public int DriverId { get; set; }
    }

    /// <summary>
    ///     Request model for updating job status.
    /// </summary>
    public class UpdateJobStatusRequest
    {
        /// <summary>
        ///     Gets or sets the new job status.
        /// </summary>
        public JobStatus Status { get; set; }
    }
}
