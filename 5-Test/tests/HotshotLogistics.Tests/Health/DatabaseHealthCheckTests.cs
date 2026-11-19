// <copyright file="DatabaseHealthCheckTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Data.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HotshotLogistics.Tests.Health
{
    /// <summary>
    ///     Tests for <see cref="DatabaseHealthCheck" />.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class DatabaseHealthCheckTests
    {
        [Fact]
        public async Task CheckHealthAsync_WithoutConnectionString_ReturnsUnhealthy()
        {
            DatabaseHealthCheck check = CreateCheck(connectionString: null);

            HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

            result.Status.Should().Be(HealthStatus.Unhealthy);
        }

        [Fact]
        public async Task CheckHealthAsync_WhenServerRefusesConnection_ReturnsUnhealthyWithoutDetails()
        {
            // Port 1 on loopback refuses immediately, so no network is touched.
            DatabaseHealthCheck check = CreateCheck(
                "Server=tcp:127.0.0.1,1;Database=leak-sentinel-db;Integrated Security=True;Encrypt=False");

            HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Exception.Should().BeNull();
            result.Description.Should().NotContain("127.0.0.1").And.NotContain("leak-sentinel-db");
        }

        [Fact]
        public async Task CheckHealthAsync_WhenCallerCancels_ThrowsOperationCanceled()
        {
            DatabaseHealthCheck check = CreateCheck(
                "Server=tcp:127.0.0.1,1;Database=none;Integrated Security=True;Encrypt=False");
            using CancellationTokenSource cancelled = new();
            await cancelled.CancelAsync();

            Func<Task> act = () => check.CheckHealthAsync(new HealthCheckContext(), cancelled.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [DatabaseFact]
        public async Task CheckHealthAsync_WithReachableDatabase_ReturnsHealthy()
        {
            DatabaseHealthCheck check = CreateCheck(TestDatabaseHelper.GetConnectionString());

            HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

            result.Status.Should().Be(HealthStatus.Healthy);
        }

        private static DatabaseHealthCheck CreateCheck(string? connectionString)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                })
                .Build();
            return new DatabaseHealthCheck(
                configuration,
                Options.Create(new DatabaseHealthCheckOptions { TimeoutSeconds = 2 }),
                NullLogger<DatabaseHealthCheck>.Instance);
        }
    }
}
