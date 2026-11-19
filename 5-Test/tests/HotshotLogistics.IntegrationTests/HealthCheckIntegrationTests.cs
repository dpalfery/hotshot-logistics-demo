// <copyright file="HealthCheckIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace HotshotLogistics.IntegrationTests
{
    /// <summary>
    ///     Integration tests for the health endpoints against the real SQL Server.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class HealthCheckIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="HealthCheckIntegrationTests" /> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public HealthCheckIntegrationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        /// <summary>
        ///     Tests that readiness is healthy when the database is reachable.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetReady_WhenDatabaseIsReachable_ReturnsHealthy()
        {
            HttpResponseMessage response = await Client.GetAsync("/health/ready");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        ///     Tests that the full report lists the database check as healthy.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetHealth_WhenDatabaseIsReachable_ListsDatabaseAsHealthy()
        {
            HttpResponseMessage response = await Client.GetAsync("/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            string content = await response.Content.ReadAsStringAsync();
            JsonElement check = JsonDocument.Parse(content).RootElement.GetProperty("checks")[0];
            check.GetProperty("name").GetString().Should().Be("database");
            check.GetProperty("status").GetString().Should().Be("Healthy");
        }
    }
}
