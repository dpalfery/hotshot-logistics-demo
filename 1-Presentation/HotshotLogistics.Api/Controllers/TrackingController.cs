// <copyright file="TrackingController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    ///     API controller for location tracking services.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class TrackingController : ApiControllerBase
    {
        private readonly ILogger<TrackingController> _logger;
        private readonly ITrackingService _trackingService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TrackingController" /> class.
        /// </summary>
        /// <param name="trackingService">The tracking service.</param>
        /// <param name="logger">The _logger.</param>
        public TrackingController(
            ITrackingService trackingService,
            ILogger<TrackingController> logger)
        {
            _trackingService = trackingService ?? throw new ArgumentNullException(nameof(trackingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Starts tracking for a job and driver.
        /// </summary>
        /// <param name="request">The start tracking request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Success status of starting tracking.</returns>
        [HttpPost("start")]
        [ProducesResponseType(typeof(TrackingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrackingResult>> StartTracking(
            [FromBody] StartTrackingRequest? request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequestProblem("Start tracking request is required");
                }

                if (string.IsNullOrWhiteSpace(request.JobId))
                {
                    return BadRequestProblem("Job ID is required");
                }

                if (request.DriverId <= 0)
                {
                    return BadRequestProblem("Valid driver ID is required");
                }

                bool success =
                    await _trackingService.StartTrackingAsync(request.JobId, request.DriverId, cancellationToken);

                TrackingResult result = new()
                {
                    Success = success,
                    JobId = request.JobId,
                    DriverId = request.DriverId,
                    Message = success ? "Tracking started successfully" : "Failed to start tracking",
                    Timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid start tracking request: {Message}", ex.Message);
                return BadRequestProblem(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Job or driver not found: {Message}", ex.Message);
                return NotFoundProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Stops tracking for a job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Success status of stopping tracking.</returns>
        [HttpPost("stop/{jobId}")]
        [ProducesResponseType(typeof(TrackingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrackingResult>> StopTracking(string jobId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jobId))
                {
                    return BadRequestProblem("Job ID is required");
                }

                bool success = await _trackingService.StopTrackingAsync(jobId, cancellationToken);

                TrackingResult result = new()
                {
                    Success = success,
                    JobId = jobId,
                    Message = success ? "Tracking stopped successfully" : "Failed to stop tracking",
                    Timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid stop tracking request for job: {Message}", ex.Message);
                return BadRequestProblem(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Job not found for stop tracking");
                return NotFoundProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Updates the location for a job and driver.
        /// </summary>
        /// <param name="request">The location update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created location tracking record.</returns>
        [HttpPost("location")]
        [ProducesResponseType(typeof(LocationTracking), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LocationTracking>> UpdateLocation(
            [FromBody] UpdateLocationRequest? request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequestProblem("Location update request is required");
                }

                if (string.IsNullOrWhiteSpace(request.JobId))
                {
                    return BadRequestProblem("Job ID is required");
                }

                if (request.DriverId <= 0)
                {
                    return BadRequestProblem("Valid driver ID is required");
                }

                if (request.LocationUpdate == null)
                {
                    return BadRequestProblem("Location update data is required");
                }

                LocationTracking locationTracking = await _trackingService.UpdateLocationAsync(
                    request.JobId,
                    request.DriverId,
                    request.LocationUpdate,
                    cancellationToken);

                return CreatedAtAction(
                    nameof(GetCurrentLocation),
                    new { jobId = request.JobId },
                    locationTracking);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid location update request: {Message}", ex.Message);
                return BadRequestProblem(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Job or driver not found for location update: {Message}", ex.Message);
                return NotFoundProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Gets the current location for a job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest location tracking record.</returns>
        [HttpGet("location/{jobId}")]
        [ProducesResponseType(typeof(LocationTracking), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LocationTracking>> GetCurrentLocation(string jobId,
            CancellationToken cancellationToken = default)
        {
            LocationTracking? locationTracking =
                await _trackingService.GetCurrentLocationAsync(jobId, cancellationToken);
            if (locationTracking == null)
            {
                return NotFoundProblem($"No location tracking found for job {jobId}");
            }

            return Ok(locationTracking);
        }

        /// <summary>
        ///     Gets the location history for a job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="startTime">The start time for the history.</param>
        /// <param name="endTime">The end time for the history.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The location tracking history.</returns>
        [HttpGet("history/{jobId}")]
        [ProducesResponseType(typeof(IEnumerable<LocationTracking>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<LocationTracking>>> GetLocationHistory(
            string jobId,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequestProblem("Job ID is required");
            }

            // Default to last 24 hours if no time range specified
            DateTime start = startTime ?? DateTime.UtcNow.AddDays(-1);
            DateTime end = endTime ?? DateTime.UtcNow;

            if (start >= end)
            {
                return BadRequestProblem("Start time must be before end time");
            }

            IEnumerable<LocationTracking> locationHistory =
                await _trackingService.GetLocationHistoryAsync(jobId, start, end, cancellationToken);
            return Ok(locationHistory);
        }

        /// <summary>
        ///     Checks if a driver has deviated from the expected route.
        /// </summary>
        /// <param name="request">The route deviation check request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Route deviation status.</returns>
        [HttpPost("check-deviation")]
        [ProducesResponseType(typeof(RouteDeviationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RouteDeviationResult>> CheckRouteDeviation(
            [FromBody] RouteDeviationRequest? request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequestProblem("Route deviation request is required");
                }

                if (string.IsNullOrWhiteSpace(request.JobId))
                {
                    return BadRequestProblem("Job ID is required");
                }

                if (request.CurrentLocation == null)
                {
                    return BadRequestProblem("Current location is required");
                }

                bool hasDeviated = await _trackingService.CheckRouteDeviationAsync(
                    request.JobId,
                    request.CurrentLocation,
                    cancellationToken);

                RouteDeviationResult result = new()
                {
                    JobId = request.JobId,
                    HasDeviated = hasDeviated,
                    CurrentLocation = request.CurrentLocation,
                    CheckedAt = DateTime.UtcNow,
                    Message = hasDeviated ? "Driver has deviated from expected route" : "Driver is on expected route"
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid route deviation request: {Message}", ex.Message);
                return BadRequestProblem(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Job not found for route deviation check: {Message}", ex.Message);
                return NotFoundProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Gets a public tracking link for a job (for customer access).
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Public tracking information.</returns>
        [HttpGet("public/{jobId}")]
        [EnableRateLimiting(RateLimitPolicies.Public)]
        [ProducesResponseType(typeof(PublicTrackingInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PublicTrackingInfo>> GetPublicTrackingInfo(string jobId,
            CancellationToken cancellationToken = default)
        {
            LocationTracking? currentLocation =
                await _trackingService.GetCurrentLocationAsync(jobId, cancellationToken);
            if (currentLocation == null)
            {
                return NotFoundProblem($"No tracking information available for job {jobId}");
            }

            PublicTrackingInfo publicInfo = new()
            {
                JobId = jobId,
                CurrentLatitude = (double)currentLocation.Latitude,
                CurrentLongitude = (double)currentLocation.Longitude,
                LastUpdated = currentLocation.Timestamp,
                Status = "In Transit", // This could be derived from job status
                EstimatedArrival = currentLocation.Timestamp.AddHours(2) // Placeholder calculation
            };

            return Ok(publicInfo);
        }
    }

    /// <summary>
    ///     Request model for starting tracking.
    /// </summary>
    public class StartTrackingRequest
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

    /// <summary>
    ///     Request model for updating location.
    /// </summary>
    public class UpdateLocationRequest
    {
        /// <summary>
        ///     Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the driver ID.
        /// </summary>
        public int DriverId { get; set; }

        /// <summary>
        ///     Gets or sets the location update data.
        /// </summary>
        public LocationUpdate? LocationUpdate { get; set; }
    }

    /// <summary>
    ///     Request model for checking route deviation.
    /// </summary>
    public class RouteDeviationRequest
    {
        /// <summary>
        ///     Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the current location.
        /// </summary>
        public LocationUpdate? CurrentLocation { get; set; }
    }

    /// <summary>
    ///     Result model for tracking operations.
    /// </summary>
    public class TrackingResult
    {
        /// <summary>
        ///     Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the driver ID.
        /// </summary>
        public int? DriverId { get; set; }

        /// <summary>
        ///     Gets or sets the result message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the operation timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    ///     Result model for route deviation checks.
    /// </summary>
    public class RouteDeviationResult
    {
        /// <summary>
        ///     Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets a value indicating whether the driver has deviated from route.
        /// </summary>
        public bool HasDeviated { get; set; }

        /// <summary>
        ///     Gets or sets the current location.
        /// </summary>
        public LocationUpdate CurrentLocation { get; set; } = new();

        /// <summary>
        ///     Gets or sets the check timestamp.
        /// </summary>
        public DateTime CheckedAt { get; set; }

        /// <summary>
        ///     Gets or sets the result message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Public tracking information for customer access.
    /// </summary>
    public class PublicTrackingInfo
    {
        /// <summary>
        ///     Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the current latitude.
        /// </summary>
        public double CurrentLatitude { get; set; }

        /// <summary>
        ///     Gets or sets the current longitude.
        /// </summary>
        public double CurrentLongitude { get; set; }

        /// <summary>
        ///     Gets or sets the last updated timestamp.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        ///     Gets or sets the current status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the estimated arrival time.
        /// </summary>
        public DateTime? EstimatedArrival { get; set; }
    }
}
