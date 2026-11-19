using System.Net;
using System.Text.Json;
using FluentAssertions;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Proves the global and per-endpoint limits answer 429 and that health probes are exempt.
    ///     The test host has no remote address, so every request shares one partition.
    /// </summary>
    public class RateLimitingTests
    {
        private const string s_driversEndpoint = "/api/Drivers";

        [Fact]
        public async Task GetDrivers_WhenGlobalLimitIsExceeded_Returns429ProblemWithRetryAfter()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory(globalLimit: 2, publicLimit: 100);
            using HttpClient client = factory.CreateAuthenticatedClient();

            using HttpResponseMessage first = await client.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);
            using HttpResponseMessage second = await client.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);
            using HttpResponseMessage third = await client.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);

            first.StatusCode.Should().Be(HttpStatusCode.OK);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            third.Headers.RetryAfter.Should().NotBeNull();
            third.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            string raw = await third.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            JsonElement body = JsonDocument.Parse(raw).RootElement;
            body.GetProperty("status").GetInt32().Should().Be(429);
            body.TryGetProperty("correlationId", out _).Should().BeTrue();
        }

        [Fact]
        public async Task GetHealthEndpoints_WhenGlobalLimitIsExhausted_AreNeverThrottled()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory(globalLimit: 1, publicLimit: 100);
            using HttpClient authenticated = factory.CreateAuthenticatedClient();
            using HttpClient anonymous = factory.CreateClient();
            using HttpResponseMessage consumed =
                await authenticated.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);
            using HttpResponseMessage throttled =
                await authenticated.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);
            throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "the limiter must be active for the exemption to mean anything");

            foreach (string path in new[] { "/health/live", "/health/ready", "/health" })
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    using HttpResponseMessage response =
                        await anonymous.GetAsync(path, TestContext.Current.CancellationToken);

                    response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"{path} is exempt");
                }
            }
        }

        [Fact]
        public async Task GetPublicTracking_WhenPublicLimitIsExceeded_Returns429WhileOtherEndpointsStillServe()
        {
            Mock<ITrackingService> trackingService = new();
            trackingService.Setup(s => s.GetCurrentLocationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocationTracking?)null);
            Mock<IDriverService> driverService = new();
            driverService.Setup(s => s.GetDriversAsync()).ReturnsAsync([]);
            await using WebApplicationFactory<Program> factory = CreateFactory(
                globalLimit: 100,
                publicLimit: 1,
                configureServices: services =>
                {
                    services.ReplaceScoped(trackingService.Object);
                    services.ReplaceScoped(driverService.Object);
                });
            using HttpClient client = factory.CreateAuthenticatedClient();

            using HttpResponseMessage first =
                await client.GetAsync("/api/Tracking/public/job-1", TestContext.Current.CancellationToken);
            using HttpResponseMessage second =
                await client.GetAsync("/api/Tracking/public/job-1", TestContext.Current.CancellationToken);
            using HttpResponseMessage other =
                await client.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);

            first.StatusCode.Should().Be(HttpStatusCode.NotFound);
            second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            other.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Startup_WithNonPositiveLimit_FailsFast()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory(globalLimit: 0, publicLimit: 10);

            Func<HttpClient> start = factory.CreateClient;

            start.Should().Throw<OptionsValidationException>().Which.Message.Should().Contain("GlobalPermitLimit");
        }

        private static WebApplicationFactory<Program> CreateFactory(
            int globalLimit,
            int publicLimit,
            Action<Microsoft.Extensions.DependencyInjection.IServiceCollection>? configureServices = null)
        {
            Mock<IDriverService> driverService = new();
            driverService.Setup(s => s.GetDriversAsync()).ReturnsAsync([]);
            return ApiHostFactory.Create(
                new Dictionary<string, string?>
                {
                    ["RateLimiting:GlobalPermitLimit"] = globalLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["RateLimiting:PublicPermitLimit"] = publicLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["RateLimiting:GlobalWindowSeconds"] = "60",
                    ["RateLimiting:PublicWindowSeconds"] = "60",
                    ["ConnectionStrings:DefaultConnection"] = string.Empty,
                },
                configureServices ?? (services => services.ReplaceScoped(driverService.Object)));
        }
    }
}
