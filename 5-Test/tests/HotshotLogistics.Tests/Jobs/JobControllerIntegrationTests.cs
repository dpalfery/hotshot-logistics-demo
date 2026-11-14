// <copyright file="JobControllerIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HotshotLogistics.Tests.Jobs
{
    /// <summary>
    ///     Integration tests for the JobController.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class JobControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="JobControllerIntegrationTests" /> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public JobControllerIntegrationTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
            // Set up test authentication
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        /// <summary>
        ///     Tests that GetJobById returns a specific job when it exists.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [DatabaseFact]
        public async Task GetJobById_WhenJobExists_ReturnsOkWithJob()
        {
            // Arrange
            // This job ID is known to exist from the SeedLargeTestData migration
            string jobId = "job-cust-001-001";

            // Act
            HttpResponseMessage response = await Client.GetAsync($"/api/Job/{jobId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            string content = await response.Content.ReadAsStringAsync();
            Job? job = JsonSerializer.Deserialize<Job>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            job.Should().NotBeNull();
            job.Id.Should().Be(jobId);
            job.CustomerId.Should().Be("cust-001");
        }

        /// <summary>
        ///     Tests that GetJobById returns NotFound for a non-existent job.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [DatabaseFact]
        public async Task GetJobById_WhenJobDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            string jobId = "job-that-does-not-exist";

            // Act
            HttpResponseMessage response = await Client.GetAsync($"/api/Job/{jobId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
