// <copyright file="TelemetryExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HotshotLogistics.Api.Telemetry
{
    /// <summary>
    ///     Registers OpenTelemetry tracing and metrics, and the Application Insights export when it is configured.
    /// </summary>
    public static class TelemetryExtensions
    {
        /// <summary>
        ///     The setting that holds the Application Insights connection string.
        /// </summary>
        public const string ConnectionStringKey = "Telemetry:ApplicationInsights:ConnectionString";

        /// <summary>
        ///     The setting that names the service in traces and metrics.
        /// </summary>
        public const string ServiceNameKey = "Telemetry:ServiceName";

        private const string s_defaultServiceName = "HotshotLogistics.Api";

        // Azure App Service and Container Apps set this variable for an attached Application Insights resource.
        private const string s_azureConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

        /// <summary>
        ///     Adds ASP.NET Core, outbound HTTP and runtime instrumentation for traces and metrics. The Application
        ///     Insights exporter is added only when a connection string is configured, so a development machine
        ///     exports nothing. The connection string comes from configuration (environment, Key Vault or user
        ///     secrets), never from code.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddApiTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            string serviceName = configuration[ServiceNameKey] is { Length: > 0 } configured
                ? configured
                : s_defaultServiceName;
            string? connectionString = GetConnectionString(configuration);

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName))
                .WithTracing(tracing =>
                {
                    tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                    if (connectionString is not null)
                    {
                        tracing.AddAzureMonitorTraceExporter(options => options.ConnectionString = connectionString);
                    }
                })
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation();
                    if (connectionString is not null)
                    {
                        metrics.AddAzureMonitorMetricExporter(options => options.ConnectionString = connectionString);
                    }
                });
            return services;
        }

        private static string? GetConnectionString(IConfiguration configuration)
        {
            string? connectionString = configuration[ConnectionStringKey];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = configuration[s_azureConnectionStringKey];
            }

            return string.IsNullOrWhiteSpace(connectionString) ? null : connectionString;
        }
    }
}
