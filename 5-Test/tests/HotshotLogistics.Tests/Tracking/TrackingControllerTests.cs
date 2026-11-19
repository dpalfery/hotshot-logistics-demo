// <copyright file="TrackingControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Api.Controllers;
using HotshotLogistics.Api.Models;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Tracking
{
    /// <summary>
    ///     Integration tests for the TrackingController.
    /// </summary>
    public class TrackingControllerTests
    {
        private readonly TrackingController _controller;
        private readonly Mock<ITrackingService> _mockTrackingService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TrackingControllerTests" /> class.
        /// </summary>
        public TrackingControllerTests()
        {
            _mockTrackingService = new Mock<ITrackingService>();
            Mock<ILogger<TrackingController>> mockLogger = new();
            _controller = new TrackingController(_mockTrackingService.Object, mockLogger.Object);
        }

        /// <summary>
        ///     Tests that StartTracking starts tracking successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StartTracking_WithValidRequest_StartsTrackingSuccessfully()
        {
            // Arrange
            StartTrackingRequest request = new()
            {
                JobId = "job-123",
                DriverId = 456
            };

            _mockTrackingService.Setup(s =>
                    s.StartTrackingAsync(request.JobId, request.DriverId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            ActionResult<TrackingResult> result = await _controller.StartTracking(request);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            TrackingResult trackingResult = okResult.Value.Should().BeOfType<TrackingResult>().Subject;
            trackingResult.Success.Should().BeTrue();
            trackingResult.JobId.Should().Be(request.JobId);
            trackingResult.DriverId.Should().Be(request.DriverId);
        }

        /// <summary>
        ///     Tests that StartTracking returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StartTracking_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            ActionResult<TrackingResult> result = await _controller.StartTracking(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that StartTracking returns BadRequest for empty job ID.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StartTracking_WithEmptyJobId_ReturnsBadRequest()
        {
            // Arrange
            StartTrackingRequest request = new()
            {
                JobId = "",
                DriverId = 456
            };

            // Act
            ActionResult<TrackingResult> result = await _controller.StartTracking(request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that StartTracking returns BadRequest for invalid driver ID.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StartTracking_WithInvalidDriverId_ReturnsBadRequest()
        {
            // Arrange
            StartTrackingRequest request = new()
            {
                JobId = "job-123",
                DriverId = 0
            };

            // Act
            ActionResult<TrackingResult> result = await _controller.StartTracking(request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that StopTracking stops tracking successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StopTracking_WithValidJobId_StopsTrackingSuccessfully()
        {
            // Arrange
            string jobId = "job-123";

            _mockTrackingService.Setup(s => s.StopTrackingAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            ActionResult<TrackingResult> result = await _controller.StopTracking(jobId);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            TrackingResult trackingResult = okResult.Value.Should().BeOfType<TrackingResult>().Subject;
            trackingResult.Success.Should().BeTrue();
            trackingResult.JobId.Should().Be(jobId);
        }

        /// <summary>
        ///     Tests that StopTracking returns BadRequest for empty job ID.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StopTracking_WithEmptyJobId_ReturnsBadRequest()
        {
            // Act
            ActionResult<TrackingResult> result = await _controller.StopTracking("");

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that UpdateLocation updates location successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateLocation_WithValidRequest_UpdatesLocationSuccessfully()
        {
            // Arrange
            UpdateLocationRequest request = new()
            {
                JobId = "job-123",
                DriverId = 456,
                LocationUpdate = new LocationUpdate
                {
                    Latitude = 40.7128m,
                    Longitude = -74.0060m,
                    Timestamp = DateTime.UtcNow
                }
            };

            LocationTracking expectedLocationTracking = CreateTestLocationTracking(request.JobId, request.DriverId);

            _mockTrackingService.Setup(s => s.UpdateLocationAsync(
                    request.JobId,
                    request.DriverId,
                    request.LocationUpdate,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLocationTracking);

            // Act
            ActionResult<LocationTracking> result = await _controller.UpdateLocation(request);

            // Assert
            result.Should().NotBeNull();
            CreatedAtActionResult createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            LocationTracking locationTracking = createdResult.Value.Should().BeAssignableTo<LocationTracking>().Subject;
            locationTracking.JobId.Should().Be(request.JobId);
        }

        /// <summary>
        ///     Tests that UpdateLocation returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateLocation_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            ActionResult<LocationTracking> result = await _controller.UpdateLocation(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that GetCurrentLocation returns current location.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCurrentLocation_WhenLocationExists_ReturnsCurrentLocation()
        {
            // Arrange
            string jobId = "job-123";
            LocationTracking expectedLocation = CreateTestLocationTracking(jobId, 456);

            _mockTrackingService.Setup(s => s.GetCurrentLocationAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLocation);

            // Act
            ActionResult<LocationTracking> result = await _controller.GetCurrentLocation(jobId);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            LocationTracking locationTracking = okResult.Value.Should().BeAssignableTo<LocationTracking>().Subject;
            locationTracking.JobId.Should().Be(jobId);
        }

        /// <summary>
        ///     Tests that GetCurrentLocation returns NotFound when no location exists.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCurrentLocation_WhenLocationNotFound_ReturnsNotFound()
        {
            // Arrange
            string jobId = "job-123";

            _mockTrackingService.Setup(s => s.GetCurrentLocationAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocationTracking?)null);

            // Act
            ActionResult<LocationTracking> result = await _controller.GetCurrentLocation(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        ///     Tests that GetLocationHistory returns location history.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetLocationHistory_WithValidParameters_ReturnsLocationHistory()
        {
            // Arrange
            string jobId = "job-123";
            DateTime startTime = DateTime.UtcNow.AddHours(-2);
            DateTime endTime = DateTime.UtcNow;
            List<LocationTracking> expectedHistory =
            [
                CreateTestLocationTracking(jobId, 456),
                CreateTestLocationTracking(jobId, 456)
            ];

            _mockTrackingService.Setup(s =>
                    s.GetLocationHistoryAsync(jobId, startTime, endTime, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedHistory);

            // Act
            ActionResult<IEnumerable<LocationTracking>> result =
                await _controller.GetLocationHistory(jobId, startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            IEnumerable<LocationTracking> history =
                okResult.Value.Should().BeAssignableTo<IEnumerable<LocationTracking>>().Subject;
            history.Should().HaveCount(2);
        }

        /// <summary>
        ///     Tests that GetLocationHistory returns BadRequest for invalid time range.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetLocationHistory_WithInvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            string jobId = "job-123";
            DateTime startTime = DateTime.UtcNow;
            DateTime endTime = DateTime.UtcNow.AddHours(-1); // End time before start time

            // Act
            ActionResult<IEnumerable<LocationTracking>> result =
                await _controller.GetLocationHistory(jobId, startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that CheckRouteDeviation checks deviation successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CheckRouteDeviation_WithValidRequest_ChecksDeviationSuccessfully()
        {
            // Arrange
            RouteDeviationRequest request = new()
            {
                JobId = "job-123",
                CurrentLocation = new LocationUpdate
                {
                    Latitude = 40.7128m,
                    Longitude = -74.0060m,
                    Timestamp = DateTime.UtcNow
                }
            };

            _mockTrackingService.Setup(s => s.CheckRouteDeviationAsync(
                    request.JobId,
                    request.CurrentLocation,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // No deviation

            // Act
            ActionResult<RouteDeviationResult> result = await _controller.CheckRouteDeviation(request);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            RouteDeviationResult deviationResult = okResult.Value.Should().BeOfType<RouteDeviationResult>().Subject;
            deviationResult.JobId.Should().Be(request.JobId);
            deviationResult.HasDeviated.Should().BeFalse();
        }

        /// <summary>
        ///     Tests that CheckRouteDeviation returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CheckRouteDeviation_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            ActionResult<RouteDeviationResult> result = await _controller.CheckRouteDeviation(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that GetPublicTrackingInfo returns public tracking information.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetPublicTrackingInfo_WhenLocationExists_ReturnsPublicTrackingInfo()
        {
            // Arrange
            string jobId = "job-123";
            LocationTracking expectedLocation = CreateTestLocationTracking(jobId, 456);

            _mockTrackingService.Setup(s => s.GetCurrentLocationAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLocation);

            // Act
            ActionResult<PublicTrackingInfo> result = await _controller.GetPublicTrackingInfo(jobId);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            PublicTrackingInfo publicInfo = okResult.Value.Should().BeOfType<PublicTrackingInfo>().Subject;
            publicInfo.JobId.Should().Be(jobId);
            publicInfo.CurrentLatitude.Should().Be((double)expectedLocation.Latitude);
            publicInfo.CurrentLongitude.Should().Be((double)expectedLocation.Longitude);
        }

        /// <summary>
        ///     Tests that GetPublicTrackingInfo returns NotFound when no location exists.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetPublicTrackingInfo_WhenLocationNotFound_ReturnsNotFound()
        {
            // Arrange
            string jobId = "job-123";

            _mockTrackingService.Setup(s => s.GetCurrentLocationAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocationTracking?)null);

            // Act
            ActionResult<PublicTrackingInfo> result = await _controller.GetPublicTrackingInfo(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        ///     Creates a test location tracking record for testing purposes.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="driverId">The driver ID.</param>
        /// <returns>A test location tracking instance.</returns>
        private static LocationTracking CreateTestLocationTracking(string jobId, int driverId)
        {
            return new LocationTracking
            {
                Id = 1L,
                JobId = jobId,
                DriverId = driverId,
                Latitude = 40.7128m,
                Longitude = -74.0060m,
                Timestamp = DateTime.UtcNow,
                Speed = 55.0m,
                Heading = 90
            };
        }
    }
}
