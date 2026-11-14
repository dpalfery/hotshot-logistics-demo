// <copyright file="TrackingControllerIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace HotshotLogistics.IntegrationTests
{
    /// <summary>
    ///     Integration tests for the TrackingController.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class TrackingControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TrackingControllerIntegrationTests" /> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public TrackingControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory)
        {
            // Set the test authentication header
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        /// <summary>
        ///     Tests that GetCurrentLocation returns NotFound for a job that is not being tracked.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCurrentLocation_WhenJobNotTracked_ReturnsNotFound()
        {
            // Arrange
            string jobId = $"test-untracked-{Guid.NewGuid():N}"; // Never has location rows: nothing writes them

            // Act
            HttpResponseMessage response = await Client.GetAsync($"/api/Tracking/location/{jobId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
