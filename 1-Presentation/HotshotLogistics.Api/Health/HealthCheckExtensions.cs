// <copyright file="HealthCheckExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Data.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotshotLogistics.Api.Health
{
    /// <summary>
    ///     Registers the health checks and maps the three health endpoints.
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        ///     The tag that marks a check as part of readiness.
        /// </summary>
        public const string ReadyTag = "ready";

        /// <summary>
        ///     The path of the full health report.
        /// </summary>
        public const string HealthPath = "/health";

        /// <summary>
        ///     The path of the liveness probe: the process is up and serving.
        /// </summary>
        public const string LivePath = "/health/live";

        /// <summary>
        ///     The path of the readiness probe: the dependencies this process needs are reachable.
        /// </summary>
        public const string ReadyPath = "/health/ready";

        /// <summary>
        ///     Adds the dependency checks. The only dependency is the database; the API has no queue or
        ///     downstream API it must reach before serving.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddApiHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<DatabaseHealthCheckOptions>(configuration.GetSection(DatabaseHealthCheckOptions.SectionName));
            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("database", HealthStatus.Unhealthy, [ReadyTag]);
            return services;
        }

        /// <summary>
        ///     Maps <c>/health</c>, <c>/health/live</c> and <c>/health/ready</c>. They are anonymous so an
        ///     orchestrator can probe them, and they report check names and statuses only.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder MapApiHealthChecks(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks(HealthPath, new HealthCheckOptions
            {
                ResponseWriter = HealthResponseWriter.WriteAsync
            });
            endpoints.MapHealthChecks(LivePath, new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = HealthResponseWriter.WriteAsync
            });
            endpoints.MapHealthChecks(ReadyPath, new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains(ReadyTag),
                ResponseWriter = HealthResponseWriter.WriteAsync
            });
            return endpoints;
        }
    }
}
