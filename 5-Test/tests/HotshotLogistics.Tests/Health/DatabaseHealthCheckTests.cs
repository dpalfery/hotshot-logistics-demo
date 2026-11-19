// <copyright file="DatabaseHealthCheckTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Data;
using FluentAssertions;
using HotshotLogistics.Data;
using HotshotLogistics.Data.Health;
using Microsoft.Data.SqlClient;
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

        [Fact]
        public async Task CheckHealthAsync_WhenFactoryFails_ReturnsUnhealthyWithoutDetails()
        {
            FakeConnectionFactory factory = new(_ => throw new InvalidOperationException("leak-sentinel-detail"));
            DatabaseHealthCheck check = CreateCheck(factory);

            HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

            factory.OpenCalls.Should().Be(1);
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Exception.Should().BeNull();
            result.Description.Should().NotContain("leak-sentinel-detail");
        }

        [Fact]
        public async Task CheckHealthAsync_WhenFactoryOutlastsTheTimeout_ReturnsUnhealthy()
        {
            FakeConnectionFactory factory = new(async token =>
            {
                await Task.Delay(Timeout.Infinite, token);
                throw new InvalidOperationException("unreachable");
            });
            DatabaseHealthCheck check = CreateCheck(factory);

            HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

            result.Status.Should().Be(HealthStatus.Unhealthy);
        }

        [DatabaseFact]
        public async Task CheckHealthAsync_WithReachableDatabase_OpensThroughTheFactoryAndReturnsHealthy()
        {
            SqlConnection? opened = null;
            FakeConnectionFactory factory = new(async token =>
            {
                opened = new SqlConnection(TestDatabaseHelper.GetConnectionString());
                await opened.OpenAsync(token);
                return opened;
            });
            DatabaseHealthCheck check = CreateCheck(factory);

            HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

            result.Status.Should().Be(HealthStatus.Healthy);
            factory.OpenCalls.Should().Be(1);
            opened!.State.Should().Be(ConnectionState.Closed, "the check owns and disposes the connection it was given");
        }

        private static DatabaseHealthCheck CreateCheck(string connectionString)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                })
                .Build();
            return CreateCheck(new SqlConnectionFactory(configuration));
        }

        private static DatabaseHealthCheck CreateCheck(ISqlConnectionFactory factory)
        {
            return new DatabaseHealthCheck(
                factory,
                Options.Create(new DatabaseHealthCheckOptions { TimeoutSeconds = 1 }),
                NullLogger<DatabaseHealthCheck>.Instance);
        }

        private sealed class FakeConnectionFactory(Func<CancellationToken, Task<SqlConnection>> open)
            : ISqlConnectionFactory
        {
            public int OpenCalls { get; private set; }

            public Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
            {
                OpenCalls++;
                return open(cancellationToken);
            }
        }
    }
}
