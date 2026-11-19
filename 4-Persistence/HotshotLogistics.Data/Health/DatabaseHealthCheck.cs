// <copyright file="DatabaseHealthCheck.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotshotLogistics.Data.Health
{
    /// <summary>
    ///     Reports whether SQL Server accepts a connection and answers a trivial query. The
    ///     connection string is read when the check runs, so a missing setting makes the database
    ///     unhealthy instead of failing host startup, and the result never carries exception text.
    /// </summary>
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseHealthCheck> _logger;
        private readonly IOptions<DatabaseHealthCheckOptions> _options;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DatabaseHealthCheck" /> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="options">The health check options.</param>
        /// <param name="logger">The logger.</param>
        public DatabaseHealthCheck(
            IConfiguration configuration,
            IOptions<DatabaseHealthCheckOptions> options,
            ILogger<DatabaseHealthCheck> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("Database health check failed: connection string 'DefaultConnection' is not set");
                return HealthCheckResult.Unhealthy("The database is not configured.");
            }

            int timeoutSeconds = Math.Max(1, _options.Value.TimeoutSeconds);
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                SqlConnectionStringBuilder builder = new(connectionString)
                {
                    ConnectTimeout = timeoutSeconds
                };

                await using SqlConnection connection = new(builder.ConnectionString);
                await connection.OpenAsync(timeout.Token);
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
