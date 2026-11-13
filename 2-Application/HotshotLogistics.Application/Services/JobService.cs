// <copyright file="JobService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Core.Exceptions;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Service for managing jobs with lifecycle management.
    /// </summary>
    public class JobService : IJobService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IDriverRepository _driverRepository;
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<JobService> _logger;
        private readonly IMappingService _mappingService;
        private readonly INotificationService _notificationService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="JobService" /> class.
        /// </summary>
        /// <param name="jobRepository">The job repository.</param>
        /// <param name="customerRepository">The customer repository.</param>
        /// <param name="driverRepository">The driver repository.</param>
        /// <param name="notificationService">The notification service.</param>
        /// <param name="mappingService">The mapping service.</param>
        /// <param name="logger">The _logger.</param>
        public JobService(
            IJobRepository jobRepository,
            ICustomerRepository customerRepository,
            IDriverRepository driverRepository,
            INotificationService notificationService,
            IMappingService mappingService,
            ILogger<JobService> logger)
        {
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _driverRepository = driverRepository ?? throw new ArgumentNullException(nameof(driverRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Job> CreateJobAsync(Job job, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating job with ID: {JobId}", job.Id);

            // Validate the job
            if (!await ValidateJobAsync(job, cancellationToken))
            {
                throw new ValidationException("Job validation failed");
            }

            // Verify customer exists and is active
            Customer? customer = await _customerRepository.GetByIdAsync(job.CustomerId, cancellationToken);
            if (customer == null || !customer.IsActive)
            {
                throw new BusinessRuleException($"Customer {job.CustomerId} not found or inactive");
            }

            // Set initial job status and timestamps
            job.Status = JobStatus.Pending;
            job.CreatedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            // Generate unique ID if not provided
            if (string.IsNullOrWhiteSpace(job.Id))
            {
                job.Id = Guid.NewGuid().ToString();
            }

            // Calculate estimated delivery time if not provided
            if (job.EstimatedDeliveryTime == default)
            {
                job.EstimatedDeliveryTime = await CalculateEstimatedDeliveryTimeAsync(job, cancellationToken);
            }

            // Initialize tracking info
            job.Tracking.CurrentStatus = "Job Created";
            job.Tracking.IsActive = false;

            Job createdJob = await _jobRepository.CreateJobAsync(job, cancellationToken);

            // Send notification for job creation
            try
            {
                await _notificationService.SendNotificationAsync(
                    job.CustomerId,
                    NotificationType.JobCreated,
                    "Job Created",
                    $"Your job '{job.Title}' has been created and is pending assignment.",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send job creation notification for job {JobId}", job.Id);
            }

            _logger.LogInformation("Job created successfully with ID: {JobId}", createdJob.Id);
            return createdJob;
        }

        /// <inheritdoc />
        public Task<JobStatusSummaryDto> GetJobStatusSummaryAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<Job?> GetJobByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return _jobRepository.GetJobByIdAsync(id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IEnumerable<Job>> GetJobsAsync(CancellationToken cancellationToken = default)
        {
            return _jobRepository.GetJobsAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<PagedResult<Job>> GetJobsAsync(
            JobFilterDto? filter = null,
            PaginationParameters? pagination = null,
            SortParameters? sort = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting jobs with filter: {@Filter}, pagination: {@Pagination}, sort: {@Sort}",
                filter, pagination, sort);

            return _jobRepository.GetJobsAsync(filter, pagination, sort, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Job?> UpdateJobAsync(string id, Job jobDetails, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating job with ID: {JobId}", id);

            Job? existingJob = await _jobRepository.GetJobByIdAsync(id, cancellationToken);
            if (existingJob == null)
            {
                _logger.LogWarning("Job not found with ID: {JobId}", id);
                return null;
            }

            // Validate the updated job details
            if (!await ValidateJobAsync(jobDetails, cancellationToken))
            {
                throw new ValidationException("Job validation failed");
            }

            jobDetails.UpdatedAt = DateTime.UtcNow;
            Job? updatedJob = await _jobRepository.UpdateJobAsync(id, jobDetails, cancellationToken);

            _logger.LogInformation("Job updated successfully with ID: {JobId}", id);
            return updatedJob;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteJobAsync(string id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting job with ID: {JobId}", id);

            Job? job = await _jobRepository.GetJobByIdAsync(id, cancellationToken);
            if (job == null)
            {
                _logger.LogWarning("Job not found with ID: {JobId}", id);
                return false;
            }

            // Check if job can be deleted (only allow deletion of jobs that haven't started)
            if (job.Status != JobStatus.Pending)
            {
                _logger.LogWarning("Cannot delete job with status: {Status}", job.Status);
                throw new BusinessRuleException($"Cannot delete job with status: {job.Status}");
            }

            bool result = await _jobRepository.DeleteJobAsync(id, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Job deleted successfully with ID: {JobId}", id);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<Job> AssignDriverAsync(string jobId, int driverId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Assigning driver {DriverId} to job {JobId}", driverId, jobId);

            Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                throw new BusinessRuleException($"Job {jobId} not found");
            }

            Driver? driver = await _driverRepository.GetDriverByIdAsync(driverId, cancellationToken);
            if (driver == null)
            {
                throw new BusinessRuleException($"Driver {driverId} not found");
            }

            // Check if driver is available
            if (!await IsDriverAvailableAsync(driverId, job.EstimatedDeliveryTime,
                    cancellationToken: cancellationToken))
            {
                throw new BusinessRuleException($"Driver {driverId} is not available for the job timeframe");
            }

            // Update job with driver assignment
            job.AssignedDriverId = driverId;
            job.Status = JobStatus.Assigned;
            job.UpdatedAt = DateTime.UtcNow;

            Job? updatedJob = await _jobRepository.UpdateJobAsync(jobId, job, cancellationToken);
            if (updatedJob == null)
            {
                throw new InvalidOperationException($"Failed to update job {jobId}");
            }

            // Send notification to driver
            try
            {
                await _notificationService.SendNotificationAsync(
                    driverId.ToString(),
                    NotificationType.JobAssignment,
                    "New Job Assignment",
                    $"You have been assigned to job {job.Title}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send job assignment notification to driver {DriverId}", driverId);
            }

            _logger.LogInformation("Driver {DriverId} assigned to job {JobId} successfully", driverId, jobId);
            return updatedJob;
        }

        /// <inheritdoc />
        public async Task<Job> UpdateJobStatusAsync(string jobId, JobStatus status,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating job {JobId} status to {Status}", jobId, status);

            Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                throw new BusinessRuleException($"Job {jobId} not found");
            }

            JobStatus previousStatus = job.Status;
            job.Status = status;
            job.UpdatedAt = DateTime.UtcNow;

            // Update specific timestamps based on status
            switch (status)
            {
                case JobStatus.EnRoute:
                    // Driver is en route to pickup or delivery
                    job.Tracking.CurrentStatus = "En route";
                    break;
                case JobStatus.Received:
                    job.Tracking.CurrentStatus = "Delivered and received";
                    job.Tracking.IsActive = false;
                    break;
            }

            Job? updatedJob = await _jobRepository.UpdateJobAsync(jobId, job, cancellationToken);
            if (updatedJob == null)
            {
                throw new InvalidOperationException($"Failed to update job {jobId}");
            }

            // Send notifications for status changes
            try
            {
                await SendStatusChangeNotificationsAsync(updatedJob, previousStatus, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send status change notifications for job {JobId}", jobId);
            }

            _logger.LogInformation("Job {JobId} status updated to {Status} successfully", jobId, status);
            return updatedJob;
        }

        /// <inheritdoc />
        public async Task<bool> ValidateJobAsync(Job job, CancellationToken cancellationToken = default)
        {
            Dictionary<string, List<string>> errors = new(StringComparer.OrdinalIgnoreCase);

            void AddError(string key, string message)
            {
                if (!errors.TryGetValue(key, out List<string>? list))
                {
                    list = new List<string>();
                    errors[key] = list;
                }

                list.Add(message);
            }

            if (string.IsNullOrWhiteSpace(job.CustomerId))
            {
                _logger.LogWarning("Job validation failed: customer ID is empty");
                AddError("CustomerId", "Customer ID is required");
            }

            if (string.IsNullOrWhiteSpace(job.Title))
            {
                _logger.LogWarning("Job validation failed: job title is empty");
                AddError("Title", "Job title is required");
            }

            if (!job.PickupLocation.IsValid())
            {
                _logger.LogWarning("Job validation failed: pickup location is invalid");
                AddError("PickupLocation", "Pickup location is invalid");
            }
            else if (!await ValidateLocationWithGeocodingAsync(job.PickupLocation, "pickup", cancellationToken))
            {
                _logger.LogWarning("Job validation failed: pickup location geocoding validation failed");
                AddError("PickupLocation", "Pickup location geocoding validation failed");
            }

            if (!job.DeliveryLocation.IsValid())
            {
                _logger.LogWarning("Job validation failed: delivery location is invalid");
                AddError("DeliveryLocation", "Delivery location is invalid");
            }
            else if (!await ValidateLocationWithGeocodingAsync(job.DeliveryLocation, "delivery", cancellationToken))
            {
                _logger.LogWarning("Job validation failed: delivery location geocoding validation failed");
                AddError("DeliveryLocation", "Delivery location geocoding validation failed");
            }

            if (!job.Cargo.IsValid())
            {
                _logger.LogWarning("Job validation failed: cargo details are invalid");
                AddError("Cargo", "Cargo details are invalid");
            }

            if (!job.Pricing.IsValid())
            {
                _logger.LogWarning("Job validation failed: pricing details are invalid");
                AddError("Pricing", "Pricing details are invalid");
            }

            if (job.ScheduledPickupTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Job validation failed: scheduled pickup time is in the past");
                AddError("ScheduledPickupTime", "Scheduled pickup time must be in the future");
            }

            if (job.EstimatedDeliveryTime <= job.ScheduledPickupTime)
            {
                _logger.LogWarning("Job validation failed: estimated delivery time must be after pickup time");
                AddError("EstimatedDeliveryTime", "Estimated delivery time must be after pickup time");
            }

            if (errors.Any())
            {
                throw new ValidationException("Job validation failed",
                    errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> IsDriverAvailableAsync(int driverId, DateTime startTime, DateTime? endTime = null,
            CancellationToken cancellationToken = default)
        {
            Driver? driver = await _driverRepository.GetDriverByIdAsync(driverId, cancellationToken);
            if (driver == null || !driver.IsActive)
            {
                return false;
            }

            // Check if driver has any conflicting jobs
            IEnumerable<Job> driverJobs = await _jobRepository.GetByDriverIdAsync(driverId, cancellationToken);
            IEnumerable<Job> activeJobs = driverJobs.Where(j =>
                j.Status == JobStatus.Assigned ||
                j.Status == JobStatus.EnRoute);

            foreach (Job activeJob in activeJobs)
            {
                DateTime jobEndTime = activeJob.ActualDeliveryTime ?? activeJob.EstimatedDeliveryTime;
                DateTime jobStartTime = activeJob.ActualPickupTime ?? activeJob.CreatedAt;

                // Check for time overlap
                if (startTime < jobEndTime && (endTime ?? startTime.AddHours(8)) > jobStartTime)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Validates a location using geocoding to ensure it's a real address.
        /// </summary>
        /// <param name="location">The location to validate.</param>
        /// <param name="locationType">The type of location (pickup/delivery) for logging.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the location is valid, false otherwise.</returns>
        private async Task<bool> ValidateLocationWithGeocodingAsync(Location location, string locationType,
            CancellationToken cancellationToken)
        {
            try
            {
                // If coordinates are already provided, validate them with reverse geocoding
                if (location.HasCoordinates)
                {
                    ReverseGeocodingResult reverseResult =
                        await _mappingService.ReverseGeocodeAsync(location.Latitude!.Value, location.Longitude!.Value,
                            cancellationToken);

                    if (!reverseResult.IsValid)
                    {
                        _logger.LogWarning(
                            "Location validation failed: reverse geocoding failed for {LocationType} coordinates",
                            locationType);
                        return false;
                    }

                    return true;
                }

                // Otherwise, geocode the address to get coordinates
                GeocodingResult geocodeResult =
                    await _mappingService.GeocodeAddressAsync(location.FullAddress, cancellationToken);
                if (!geocodeResult.IsValid)
                {
                    _logger.LogWarning(
                        "Location validation failed: geocoding failed for {LocationType} address: {Address}",
                        locationType, location.FullAddress);
                    return false;
                }

                // Update location with geocoded coordinates
                location.Latitude = geocodeResult.Latitude;
                location.Longitude = geocodeResult.Longitude;

                _logger.LogInformation(
                    "Location validated and geocoded for {LocationType}: {Address} -> ({Lat}, {Lon})",
                    locationType, location.FullAddress, location.Latitude, location.Longitude);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating location with geocoding for {LocationType}: {Address}",
                    locationType, location.FullAddress);
                // If geocoding fails, still allow the job but log the error
                return true;
            }
        }

        /// <summary>
        ///     Calculates the estimated delivery time based on job details.
        /// </summary>
        /// <param name="job">The job to calculate delivery time for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The estimated delivery time.</returns>
        private async Task<DateTime> CalculateEstimatedDeliveryTimeAsync(Job job,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Use mapping service to get accurate distance and duration
                DistanceResult distanceResult =
                    await _mappingService.CalculateDistanceAsync(job.PickupLocation, job.DeliveryLocation,
                        cancellationToken);

                double distance;
                TimeSpan travelTime;

                if (distanceResult.IsValid)
                {
                    distance = distanceResult.Distance;
                    travelTime = distanceResult.Duration;
                }
                else
                {
                    // Fallback to basic calculation if mapping service fails
                    _logger.LogWarning("Mapping service failed for distance calculation, using fallback calculation");
                    distance = job.PickupLocation.DistanceTo(job.DeliveryLocation) ??
                               100; // Default 100 miles if no coordinates
                    int averageSpeed = 55; // Average highway speed in mph
                    travelTime = TimeSpan.FromHours(distance / averageSpeed);
                }

                TimeSpan loadingTime = TimeSpan.FromHours(1); // 1 hour for loading/unloading
                TimeSpan totalTime = travelTime.Add(loadingTime);

                // Add buffer based on priority
                TimeSpan bufferTime = job.Priority switch
                {
                    JobPriority.High => TimeSpan.FromMinutes(30), // 30 minutes buffer for high priority
                    JobPriority.Medium => TimeSpan.FromHours(1.0), // 1 hour buffer for medium priority
                    JobPriority.Low => TimeSpan.FromHours(2.0), // 2 hours buffer for low priority
                    _ => TimeSpan.FromHours(1.0)
                };

                DateTime estimatedDeliveryTime = job.ScheduledPickupTime.Add(totalTime).Add(bufferTime);

                _logger.LogInformation(
                    "Calculated delivery time for job: distance={Distance} miles, travelTime={TravelTime}, totalTime={TotalTime}, buffer={Buffer}, ETA={ETA}",
                    distance, travelTime, totalTime, bufferTime, estimatedDeliveryTime);

                return estimatedDeliveryTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating estimated delivery time, using fallback calculation");

                // Fallback calculation
                double distance = job.PickupLocation.DistanceTo(job.DeliveryLocation) ?? 100;
                int averageSpeed = 55;
                double travelTimeHours = distance / averageSpeed;
                int loadingTime = 1;
                double bufferHours = job.Priority switch
                {
                    JobPriority.High => 0.5,
                    JobPriority.Medium => 1.0,
                    JobPriority.Low => 2.0,
                    _ => 1.0
                };

                return job.ScheduledPickupTime.AddHours(travelTimeHours + loadingTime + bufferHours);
            }
        }

        /// <summary>
        ///     Calculates the current estimated time of arrival for a job in progress.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The estimated time of arrival.</returns>
        public async Task<DateTime?> CalculateCurrentEtaAsync(string jobId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Calculating current ETA for job {JobId}", jobId);

            Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                _logger.LogWarning("Job not found for ETA calculation: {JobId}", jobId);
                return null;
            }

            if (job.Status == JobStatus.Received)
            {
                return null; // No ETA needed for completed/cancelled jobs
            }

            LocationUpdate? currentLocation = job.Tracking.CurrentLocation;
            if (currentLocation == null)
            {
                // No tracking data, use original estimate
                return job.EstimatedDeliveryTime;
            }

            try
            {
                DateTime eta;

                if (job.Status == JobStatus.Assigned || job.Status == JobStatus.EnRoute)
                {
                    // Driver is heading to pickup or at pickup
                    Location currentLocationAsLocation = new()
                    {
                        Latitude = currentLocation.Latitude,
                        Longitude = currentLocation.Longitude
                    };
                    DistanceResult distanceResult =
                        await _mappingService.CalculateDistanceAsync(currentLocationAsLocation, job.PickupLocation,
                            cancellationToken);
                    if (distanceResult.IsValid)
                    {
                        eta = DateTime.UtcNow.Add(distanceResult.Duration);
                    }
                    else
                    {
                        // Fallback calculation
                        double remainingDistance = currentLocation.DistanceTo(job.PickupLocation) ?? 0;
                        decimal averageSpeed = job.Tracking.GetAverageSpeed() > 0m
                            ? job.Tracking.GetAverageSpeed()!.Value
                            : 45m;
                        eta = DateTime.UtcNow.AddHours(remainingDistance / (double)averageSpeed);
                    }

                    // Add pickup time
                    eta = eta.AddMinutes(30);

                    // Add delivery time
                    DistanceResult deliveryResult =
                        await _mappingService.CalculateDistanceAsync(job.PickupLocation, job.DeliveryLocation,
                            cancellationToken);
                    if (deliveryResult.IsValid)
                    {
                        eta = eta.Add(deliveryResult.Duration);
                    }
                    else
                    {
                        // Fallback
                        double deliveryDistance = job.PickupLocation.DistanceTo(job.DeliveryLocation) ?? 0;
                        eta = eta.AddHours(deliveryDistance / 55);
                    }

                    // Add delivery time
                    eta = eta.AddMinutes(30);
                }
                else
                {
                    // Driver is heading to delivery
                    Location currentLocationAsLocation = new()
                    {
                        Latitude = currentLocation.Latitude,
                        Longitude = currentLocation.Longitude
                    };
                    DistanceResult distanceResult =
                        await _mappingService.CalculateDistanceAsync(currentLocationAsLocation, job.DeliveryLocation,
                            cancellationToken);
                    if (distanceResult.IsValid)
                    {
                        eta = DateTime.UtcNow.Add(distanceResult.Duration);
                    }
                    else
                    {
                        // Fallback calculation
                        double remainingDistance = currentLocation.DistanceTo(job.DeliveryLocation) ?? 0;
                        decimal averageSpeed = job.Tracking.GetAverageSpeed() > 0m
                            ? job.Tracking.GetAverageSpeed()!.Value
                            : 45m;
                        eta = DateTime.UtcNow.AddHours(remainingDistance / (double)averageSpeed);
                    }

                    // Add delivery time
                    eta = eta.AddMinutes(30);
                }

                _logger.LogInformation("Calculated ETA for job {JobId}: {ETA}", jobId, eta);
                return eta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating current ETA for job {JobId}, using fallback", jobId);

                // Fallback to original calculation
                Location targetLocation = job.Status == JobStatus.Assigned || job.Status == JobStatus.EnRoute
                    ? job.PickupLocation
                    : job.DeliveryLocation;

                double remainingDistance = currentLocation.DistanceTo(targetLocation) ?? 0;
                decimal averageSpeed =
                    job.Tracking.GetAverageSpeed() > 0m ? job.Tracking.GetAverageSpeed()!.Value : 45m;
                DateTime eta = DateTime.UtcNow.AddHours(remainingDistance / (double)averageSpeed);

                if (job.Status == JobStatus.Assigned || job.Status == JobStatus.EnRoute)
                {
                    eta = eta.AddMinutes(30);
                    double deliveryDistance = job.PickupLocation.DistanceTo(job.DeliveryLocation) ?? 0;
                    eta = eta.AddHours(deliveryDistance / 55).AddMinutes(30);
                }

                return eta;
            }
        }

        /// <summary>
        ///     Sends notifications for job status changes.
        /// </summary>
        /// <param name="job">The job that changed status.</param>
        /// <param name="previousStatus">The previous status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SendStatusChangeNotificationsAsync(Job job, JobStatus previousStatus,
            CancellationToken cancellationToken)
        {
            _ = previousStatus;
            string statusMessage = job.Status switch
            {
                JobStatus.Assigned => "Your job has been assigned to a driver",
                JobStatus.EnRoute => "Driver is en route",
                JobStatus.Received => "Your job has been completed and cargo received",
                _ => $"Your job status has been updated to {job.Status}"
            };

            // Notify customer
            await _notificationService.SendNotificationAsync(
                job.CustomerId,
                NotificationType.JobStatusUpdate,
                "Job Status Update",
                statusMessage,
                cancellationToken);

            // Notify driver if assigned
            if (job.AssignedDriverId.HasValue)
            {
                string driverMessage = job.Status switch
                {
                    JobStatus.EnRoute => "Please proceed to pickup/delivery location",
                    JobStatus.Received => "Job has been marked as completed and received",
                    _ => $"Job status updated to {job.Status}"
                };

                await _notificationService.SendNotificationAsync(
                    job.AssignedDriverId.Value.ToString(),
                    NotificationType.JobStatusUpdate,
                    "Job Status Update",
                    driverMessage,
                    cancellationToken);
            }
        }
    }
}
