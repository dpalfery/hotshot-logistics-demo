// <copyright file="DatabaseHealthCheck.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Contracts.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotshotLogistics.Data.Health
{
    /// <summary>
    ///     Reports whether SQL Server accepts a connection and answers a trivial query. The
    ///     connection comes from <see cref="ISqlConnectionFactory" />, and the result never carries
    ///     exception text.
    /// </summary>
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ILogger<DatabaseHealthCheck> _logger;
        private readonly IOptions<DatabaseHealthCheckOptions> _options;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DatabaseHealthCheck" /> class.
        /// </summary>
        /// <param name="connectionFactory">The factory that opens the database connection.</param>
        /// <param name="options">The health check options.</param>
        /// <param name="logger">The logger.</param>
        public DatabaseHealthCheck(
            ISqlConnectionFactory connectionFactory,
            IOptions<DatabaseHealthCheckOptions> options,
            ILogger<DatabaseHealthCheck> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            int timeoutSeconds = Math.Max(1, _options.Value.TimeoutSeconds);
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                await using SqlConnection connection = await _connectionFactory.OpenConnectionAsync(timeout.Token);
                await using SqlCommand command = new("SELECT 1", connection) { CommandTimeout = timeoutSeconds };
                await command.ExecuteScalarAsync(timeout.Token);
                return HealthCheckResult.Healthy("The database answered.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is SqlException or OperationCanceledException or ArgumentException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("The database is unreachable.");
            }
        }
    }
}
