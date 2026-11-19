using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using FluentAssertions;
using HotshotLogistics.Api.Telemetry;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Proves tracing and metrics are registered, and that the Application Insights export is on
    ///     only when a connection string is configured. Settings are overridden explicitly so the result
    ///     does not depend on the machine's user secrets or environment.
    /// </summary>
    public class TelemetryTests
    {
        // A closed loopback port: if an exporter ever flushes, nothing leaves the machine.
        private const string s_connectionString =
            "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:1/";

        [Fact]
        public async Task AddApiTelemetry_RegistersTracingAndMetricsProviders_NamedForTheService()
        {
            await using ServiceProvider provider = BuildProvider(connectionString: null);

            TracerProvider tracer = provider.GetRequiredService<TracerProvider>();
            MeterProvider meter = provider.GetRequiredService<MeterProvider>();

            tracer.GetResource().Attributes.Should().Contain(
                attribute => attribute.Key == "service.name" && (string)attribute.Value == "HotshotLogistics.Api");
            meter.GetResource().Attributes.Should().Contain(
                attribute => attribute.Key == "service.name" && (string)attribute.Value == "HotshotLogistics.Api");
        }

        [Fact]
        public async Task AddApiTelemetry_WithoutConnectionString_ConfiguresNoExporter()
        {
            await using ServiceProvider provider = BuildProvider(connectionString: null);

            _ = provider.GetRequiredService<TracerProvider>();
            _ = provider.GetRequiredService<MeterProvider>();

            provider.GetRequiredService<IOptionsMonitor<AzureMonitorExporterOptions>>().Get(Options.DefaultName).ConnectionString
                .Should().BeNullOrEmpty("no connection string means no exporter is configured");
        }

        [Fact]
        public async Task AddApiTelemetry_WithConnectionString_ConfiguresTheApplicationInsightsExporter()
        {
            await using ServiceProvider provider = BuildProvider(s_connectionString);

            _ = provider.GetRequiredService<TracerProvider>();
            _ = provider.GetRequiredService<MeterProvider>();

            provider.GetRequiredService<IOptionsMonitor<AzureMonitorExporterOptions>>().Get(Options.DefaultName).ConnectionString
                .Should().Be(s_connectionString);
        }

        [Fact]
        public async Task Host_WithoutConnectionString_StartsAndServesRequests()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory(connectionString: string.Empty);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response =
                await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task Request_WithCorrelationId_TagsTheRequestActivity()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory(connectionString: string.Empty);
            List<Activity> stopped = [];
            using ActivityListener listener = new()
            {
                ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    lock (stopped)
                    {
                        stopped.Add(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(listener);
            using HttpClient client = factory.CreateClient();
            using HttpRequestMessage request = new(HttpMethod.Get, "/health/live");
            request.Headers.Add("X-Correlation-ID", "corr-telemetry-1");

            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            lock (stopped)
            {
                stopped.Should().Contain(activity => (string?)activity.GetTagItem("correlation.id") == "corr-telemetry-1");
            }
        }

        private static ServiceProvider BuildProvider(string? connectionString)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Telemetry:ApplicationInsights:ConnectionString"] = connectionString,
                })
                .Build();
            ServiceCollection services = new();
            services.AddLogging();
            services.AddApiTelemetry(configuration);
            return services.BuildServiceProvider();
        }

        private static WebApplicationFactory<Program> CreateFactory(string connectionString)
        {
            return ApiHostFactory.Create(new Dictionary<string, string?>
            {
                ["Telemetry:ApplicationInsights:ConnectionString"] = connectionString,
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = string.Empty,
                ["ConnectionStrings:DefaultConnection"] = string.Empty,
            });
        }
    }
}
