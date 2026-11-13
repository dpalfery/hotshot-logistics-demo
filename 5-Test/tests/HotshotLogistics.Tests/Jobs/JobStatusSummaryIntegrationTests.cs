// <copyright file="JobStatusSummaryIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HotshotLogistics.Tests.Jobs
{
    /// <summary>
    ///     Integration tests for the job status summary endpoint.
    /// </summary>
    [Collection("DatabaseCollection")]
    public class JobStatusSummaryIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="JobStatusSummaryIntegrationTests" /> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public JobStatusSummaryIntegrationTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        /// <summary>
        ///     Tests that the endpoint returns a summary DTO with counts for each status.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [DatabaseFact]
        public async Task GetJobStatusSummary_WhenAvailable_ReturnsOkWithSummaryDto()
        {
            // Act
            HttpResponseMessage response = await Client.GetAsync("/api/Job/status-summary");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            string content = await response.Content.ReadAsStringAsync();
            JobStatusSummaryDto? summary = JsonSerializer.Deserialize<JobStatusSummaryDto>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            summary.Should().NotBeNull();
            summary!.Pending.Should().BeGreaterThanOrEqualTo(0);
            summary.Assigned.Should().BeGreaterThanOrEqualTo(0);
            summary.EnRoute.Should().BeGreaterThanOrEqualTo(0);
            summary.Received.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
