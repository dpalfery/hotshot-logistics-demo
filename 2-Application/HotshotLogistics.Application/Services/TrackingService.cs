// <copyright file="TrackingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;
using System.Text.Json;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Service for location tracking operations.
    /// </summary>
    public class TrackingService : ITrackingService
    {
        // Route deviation threshold in miles
        private const double s_routeDeviationThresholdMiles = 5.0;
        private readonly IDistributedCache _cache;
        private readonly IDriverRepository _driverRepository;
        private readonly IJobRepository _jobRepository;
        private readonly ILocationTrackingRepository _locationTrackingRepository;
        private readonly ILogger<TrackingService> _logger;
        private readonly INotificationService _notificationService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TrackingService" /> class.
        /// </summary>
        /// <param name="locationTrackingRepository">The location tracking repository.</param>
        /// <param name="jobRepository">The job repository.</param>
        /// <param name="driverRepository">The driver repository.</param>
        /// <param name="notificationService">The notification service.</param>
        /// <param name="cache">The distributed _cache.</param>
        /// <param name="logger">The _logger.</param>
        public TrackingService(
            ILocationTrackingRepository locationTrackingRepository,
            IJobRepository jobRepository,
            IDriverRepository driverRepository,
            INotificationService notificationService,
            IDistributedCache cache,
            ILogger<TrackingService> logger)
        {
            _locationTrackingRepository = locationTrackingRepository ??
                                          throw new ArgumentNullException(nameof(locationTrackingRepository));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _driverRepository = driverRepository ?? throw new ArgumentNullException(nameof(driverRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<bool> StartTrackingAsync(string jobId, int driverId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting tracking for job {JobId} with driver {DriverId}", jobId, driverId);

            // Validate job exists and is assigned to the driver
            Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                _logger.LogWarning("Job not found: {JobId}", jobId);
                return false;
            }

            if (job.AssignedDriverId != driverId)
            {
                _logger.LogWarning("Driver {DriverId} is not assigned to job {JobId}", driverId, jobId);
                return false;
            }

            // Validate driver exists and is active
            Driver? driver = await _driverRepository.GetDriverByIdAsync(driverId, cancellationToken);
            if (driver == null || !driver.IsActive)
            {
                _logger.LogWarning("Driver not found or inactive: {DriverId}", driverId);
                return false;
            }

            // Set tracking status in cache
            string trackingKey = $"tracking:{jobId}";
            var trackingInfo = new
            {
                JobId = jobId,
                DriverId = driverId,
                StartTime = DateTime.UtcNow,
                IsActive = true
            };

            string trackingJson = JsonSerializer.Serialize(trackingInfo);
            await _cache.SetStringAsync(trackingKey, trackingJson, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24) // Keep tracking active for 24 hours
            }, cancellationToken);

            _logger.LogInformation("Tracking started successfully for job {JobId} with driver {DriverId}", jobId,
                driverId);
            return true;
        }

        /// <inheritdoc />
        public async Task<LocationTracking> UpdateLocationAsync(string jobId, int driverId,
            LocationUpdate locationUpdate, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Updating location for job {JobId} with driver {DriverId}", jobId, driverId);

            // Validate location update
            if (!locationUpdate.IsValid())
            {
                throw new ArgumentException("Invalid location update data", nameof(locationUpdate));
            }

            Job? trackedJob = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (trackedJob == null)
            {
                throw new KeyNotFoundException($"Job not found: {jobId}");
            }

            if (trackedJob.AssignedDriverId != driverId)
            {
                throw new UnauthorizedAccessException($"Driver {driverId} is not assigned to job {jobId}");
            }

            // Check if tracking is active
            string trackingKey = $"tracking:{jobId}";
            string? trackingJson = await _cache.GetStringAsync(trackingKey, cancellationToken);
            if (string.IsNullOrEmpty(trackingJson))
            {
                throw new InvalidOperationException($"Tracking is not active for job {jobId}");
            }

            // Create location tracking record
            LocationTracking locationTracking = LocationTracking.FromLocationUpdate(jobId, driverId, locationUpdate);
            LocationTracking savedLocation = await _locationTrackingRepository.AddAsync(locationTracking, cancellationToken);

            // Update cache with latest location
            string cacheKey = $"location:current:{jobId}";
            string locationJson = JsonSerializer.Serialize(locationUpdate);
            await _cache.SetStringAsync(cacheKey, locationJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) // Cache for 30 minutes
            }, cancellationToken);

            // Update job tracking information
            try
            {
                await UpdateJobTrackingInfoAsync(jobId, locationUpdate, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update job tracking info for job {JobId}", jobId);
            }

            // Check for route deviation
            try
            {
                bool hasDeviated = await CheckRouteDeviationAsync(jobId, locationUpdate, cancellationToken);
                if (hasDeviated)
                {
                    await HandleRouteDeviationAsync(jobId, driverId, locationUpdate, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check route deviation for job {JobId}", jobId);
            }

            _logger.LogDebug("Location updated successfully for job {JobId}", jobId);
            return savedLocation;
        }

        /// <inheritdoc />
        public async Task<bool> StopTrackingAsync(string jobId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Stopping tracking for job {JobId}", jobId);

            // Remove tracking status from cache
            string trackingKey = $"tracking:{jobId}";
            await _cache.RemoveAsync(trackingKey, cancellationToken);

            // Remove current location from cache
            string locationKey = $"location:current:{jobId}";
            await _cache.RemoveAsync(locationKey, cancellationToken);

            _logger.LogInformation("Tracking stopped successfully for job {JobId}", jobId);
            return true;
        }

        /// <inheritdoc />
        public async Task<LocationTracking?> GetCurrentLocationAsync(string jobId,
            CancellationToken cancellationToken = default)
        {
            // Try to get from cache first
            string cacheKey = $"location:current:{jobId}";
            string? cachedLocationJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedLocationJson))
            {
                try
                {
                    LocationUpdate? cachedLocation = JsonSerializer.Deserialize<LocationUpdate>(cachedLocationJson);
                    if (cachedLocation != null)
                    {
                        // Get driver ID from job
                        Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
                        if (job?.AssignedDriverId.HasValue == true)
                        {
                            return LocationTracking.FromLocationUpdate(jobId, job.AssignedDriverId.Value,
                                cachedLocation);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize cached location for job {JobId}", jobId);
                }
            }

            // Fall back to database
            return await _locationTrackingRepository.GetLatestByJobIdAsync(jobId);
        }

        /// <inheritdoc />
        public Task<IEnumerable<LocationTracking>> GetLocationHistoryAsync(string jobId, DateTime startTime,
            DateTime endTime, CancellationToken cancellationToken = default)
        {
            return _locationTrackingRepository.GetByJobIdAndTimeRangeAsync(jobId, startTime, endTime);
        }

        /// <inheritdoc />
        public async Task<bool> CheckRouteDeviationAsync(string jobId, LocationUpdate currentLocation,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
                if (job == null)
                {
                    return false;
                }

                // Get expected route points (simplified - in real implementation, use mapping service)
                List<LocationUpdate> expectedRoute = await GetExpectedRouteAsync(job.PickupLocation.FullAddress,
                    job.DeliveryLocation.FullAddress);
                if (expectedRoute.Count == 0)
                {
                    return false;
                }

                // Find the closest point on the expected route
                double closestDistance = expectedRoute.Min(currentLocation.DistanceTo);

                // Check if current location is too far from the expected route
                return closestDistance > s_routeDeviationThresholdMiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking route deviation for job {JobId}", jobId);
                return false;
            }
        }

        /// <summary>
        ///     Gets the expected route between two addresses.
        /// </summary>
        /// <param name="fromAddress">The origin address.</param>
        /// <param name="toAddress">The destination address.</param>
        /// <returns>A list of location points representing the expected route.</returns>
        private static async Task<List<LocationUpdate>> GetExpectedRouteAsync(string fromAddress, string toAddress)
        {
            _ = fromAddress;
            _ = toAddress;

            // Simplified route calculation - in real implementation, use mapping service like Google Maps or Azure Maps
            // For demo purposes, return a straight line with a few waypoints

            await Task.Delay(100); // Simulate API call

            List<LocationUpdate> route = new();

            // Generate some sample waypoints (in real implementation, get from mapping service)
            Random random = new();
            decimal startLat = 40.7128m + ((decimal)(random.NextDouble() - 0.5) * 0.1m); // Around NYC
            decimal startLon = -74.0060m + ((decimal)(random.NextDouble() - 0.5) * 0.1m);
            decimal endLat = startLat + ((decimal)(random.NextDouble() - 0.5) * 0.5m);
            decimal endLon = startLon + ((decimal)(random.NextDouble() - 0.5) * 0.5m);

            // Add waypoints
            route.Add(new LocationUpdate
            {
                Latitude = startLat,
                Longitude = startLon,
                Timestamp = DateTime.UtcNow.AddHours(-2)
            });

            route.Add(new LocationUpdate
            {
                Latitude = (startLat + endLat) / 2,
                Longitude = (startLon + endLon) / 2,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            });

            route.Add(new LocationUpdate
            {
                Latitude = endLat,
                Longitude = endLon,
                Timestamp = DateTime.UtcNow
            });

            return route;
        }

        /// <summary>
        ///     Generates a public tracking link for a job.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The tracking link URL.</returns>
        public async Task<string> GenerateTrackingLinkAsync(string jobId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating tracking link for job {JobId}", jobId);

            Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                throw new ArgumentException($"Job {jobId} not found", nameof(jobId));
            }

            // Generate a secure tracking token (in real implementation, use proper token generation)
            string trackingToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{jobId}:{DateTime.UtcNow.Ticks}"));

            // Store tracking token in cache with expiration
            string tokenKey = $"tracking:token:{trackingToken}";
            await _cache.SetStringAsync(tokenKey, jobId, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) // Token valid for 7 days
            }, cancellationToken);

            // Generate tracking URL (in real implementation, use proper base URL from configuration)
            string trackingUrl = $"https://tracking.hotshotlogistics.com/track/{trackingToken}";

            _logger.LogInformation("Tracking link generated for job {JobId}: {TrackingUrl}", jobId, trackingUrl);
            return trackingUrl;
        }

        /// <summary>
        ///     Updates the job's tracking information with the latest location data.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="locationUpdate">The location update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateJobTrackingInfoAsync(string jobId, LocationUpdate locationUpdate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Updating job tracking info for job {JobId}", jobId);

            Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                _logger.LogWarning("Job not found for tracking update: {JobId}", jobId);
                return;
            }

            // Update job's tracking information
            job.Tracking.AddUpdate(locationUpdate);
            job.Tracking.LastUpdateTime = locationUpdate.Timestamp;
            job.Tracking.IsActive = true;

            // Update estimated arrival time based on current location and speed
            if (locationUpdate.Speed is > 0)
            {
                Location targetLocation = job.Status == JobStatus.Assigned || job.Status == JobStatus.EnRoute
                    ? job.PickupLocation
                    : job.DeliveryLocation;

                double? remainingDistance = locationUpdate.DistanceTo(targetLocation);
                if (remainingDistance.HasValue)
                {
                    double remainingTimeHours = remainingDistance.Value / (double)locationUpdate.Speed.Value;
                    job.Tracking.EstimatedArrival = DateTime.UtcNow.AddHours(remainingTimeHours);
                }
            }

            // Update job status based on location proximity
            await UpdateJobStatusBasedOnLocationAsync(job, locationUpdate, cancellationToken);

            // Save updated job
            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepository.UpdateJobAsync(job.Id, job, cancellationToken);

            _logger.LogDebug("Job tracking info updated for job {JobId}", jobId);
        }

        /// <summary>
        ///     Updates job status based on current location proximity to pickup/delivery locations.
        /// </summary>
        /// <param name="job">The job to update.</param>
        /// <param name="currentLocation">The current location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateJobStatusBasedOnLocationAsync(Job job, LocationUpdate currentLocation,
            CancellationToken cancellationToken)
        {
            const double proximityThresholdMiles = 0.5; // Within 0.5 miles

            double? distanceToPickup = currentLocation.DistanceTo(job.PickupLocation);
            double? distanceToDelivery = currentLocation.DistanceTo(job.DeliveryLocation);

            bool statusChanged = false;

            // Check if driver has arrived at pickup or delivery location
            if (job.Status == JobStatus.EnRoute &&
                distanceToPickup is <= proximityThresholdMiles)
            {
                job.Tracking.CurrentStatus = "Arrived at pickup location";
                statusChanged = true;
            }
            // Check if driver has arrived at delivery location
            else if (job.Status == JobStatus.EnRoute &&
                     distanceToDelivery is <= proximityThresholdMiles)
            {
                // Don't automatically mark as received - wait for driver confirmation
                job.Tracking.CurrentStatus = "Arrived at delivery location";
                statusChanged = true;
            }

            // Send notifications if status changed
            if (statusChanged)
            {
                try
                {
                    await _notificationService.SendNotificationAsync(
                        job.CustomerId,
                        NotificationType.JobStatusUpdate,
                        "Job Status Update",
                        $"Driver status updated: {job.Tracking.CurrentStatus}",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send status update notification for job {JobId}", job.Id);
                }
            }
        }

        /// <summary>
        ///     Gets tracking statistics for a job.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The tracking statistics.</returns>
        public async Task<TrackingStatistics> GetTrackingStatisticsAsync(string jobId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting tracking statistics for job {JobId}", jobId);

            Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                throw new ArgumentException($"Job {jobId} not found", nameof(jobId));
            }

            IEnumerable<LocationTracking> locationHistory = await _locationTrackingRepository.GetByJobIdAsync(jobId);
            List<LocationTracking> locations = locationHistory.ToList();

            TrackingStatistics statistics = new()
            {
                JobId = jobId,
                TotalUpdates = locations.Count,
                FirstUpdate = locations.OrderBy(l => l.Timestamp).FirstOrDefault()?.Timestamp,
                LastUpdate = locations.OrderByDescending(l => l.Timestamp).FirstOrDefault()?.Timestamp,
                TotalDistance = CalculateTotalDistance(locations),
                AverageSpeed = CalculateAverageSpeed(locations),
                MaxSpeed = locations.Where(l => l.Speed.HasValue).Max(l => l.Speed),
                IsActive = job.Tracking.IsActive
            };

            if (statistics is { FirstUpdate: not null, LastUpdate: not null })
            {
                statistics.TotalDuration = statistics.LastUpdate.Value - statistics.FirstUpdate.Value;
            }

            _logger.LogDebug(
                "Tracking statistics calculated for job {JobId}: {TotalUpdates} updates, {TotalDistance:F2} miles",
                jobId, statistics.TotalUpdates, statistics.TotalDistance);

            return statistics;
        }

        /// <summary>
        ///     Calculates the total distance traveled based on location history.
        /// </summary>
        /// <param name="locations">The location tracking records.</param>
        /// <returns>The total distance in miles.</returns>
        private static decimal CalculateTotalDistance(List<LocationTracking> locations)
        {
            if (locations.Count < 2)
            {
                return 0;
            }

            List<LocationTracking> orderedLocations = locations.OrderBy(l => l.Timestamp).ToList();
            decimal totalDistance = 0;

            for (int i = 1; i < orderedLocations.Count; i++)
            {
                LocationTracking prev = orderedLocations[i - 1];
                LocationTracking current = orderedLocations[i];

                totalDistance += (decimal)prev.DistanceTo(current);
            }

            return totalDistance;
        }

        /// <summary>
        ///     Calculates the average speed based on location history.
        /// </summary>
        /// <param name="locations">The location tracking records.</param>
        /// <returns>The average speed in mph.</returns>
        private static decimal? CalculateAverageSpeed(List<LocationTracking> locations)
        {
            List<LocationTracking> locationsWithSpeed = locations.Where(l => l.Speed.HasValue).ToList();

            if (locationsWithSpeed.Count == 0)
            {
                return null;
            }

            return locationsWithSpeed.Average(l => l.Speed!.Value);
        }

        /// <summary>
        ///     Handles route deviation by sending notifications.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="driverId">The driver identifier.</param>
        /// <param name="currentLocation">The current location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleRouteDeviationAsync(string jobId, int driverId, LocationUpdate currentLocation,
            CancellationToken cancellationToken)
        {
            _logger.LogWarning("Route deviation detected for job {JobId} at location {Lat}, {Lon}",
                jobId, currentLocation.Latitude, currentLocation.Longitude);

            Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                return;
            }

            // Send notification to customer
            try
            {
                await _notificationService.SendNotificationAsync(
                    job.CustomerId,
                    NotificationType.RouteDeviation,
                    "Route Deviation Alert",
                    $"Driver for job {job.Title} has deviated from the expected route",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send route deviation notification to customer for job {JobId}",
                    jobId);
            }

            // Send notification to driver
            try
            {
                await _notificationService.SendNotificationAsync(
                    driverId.ToString(),
                    NotificationType.RouteDeviation,
                    "Route Deviation Alert",
                    "You have deviated from the expected route. Please check your navigation.",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send route deviation notification to driver {DriverId}", driverId);
            }
        }
    }
}
