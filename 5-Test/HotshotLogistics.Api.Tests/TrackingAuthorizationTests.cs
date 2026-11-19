using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Proves who may call the tracking endpoints: everything needs a signed-in caller except the
    ///     public lookup a customer's tracking page uses, and that lookup returns public fields only.
    /// </summary>
    public class TrackingAuthorizationTests
    {
        private const string s_jobId = "job-1";

        private static readonly string[] s_publicFields =
        [
            "jobId", "currentLatitude", "currentLongitude", "lastUpdated", "status", "estimatedArrival",
        ];

        public static TheoryData<string, string, string?> ProtectedEndpoints => new()
        {
            { "POST", "/api/Tracking/start", "{\"jobId\":\"job-1\",\"driverId\":7}" },
            { "POST", "/api/Tracking/stop/job-1", null },
            {
                "POST",
                "/api/Tracking/location",
                "{\"jobId\":\"job-1\",\"driverId\":7,\"locationUpdate\":{\"latitude\":40.5,\"longitude\":-74.25}}"
            },
            { "GET", "/api/Tracking/location/job-1", null },
            { "GET", "/api/Tracking/history/job-1", null },
            {
                "POST",
                "/api/Tracking/check-deviation",
                "{\"jobId\":\"job-1\",\"currentLocation\":{\"latitude\":40.5,\"longitude\":-74.25}}"
            },
        };

        [Theory]
        [MemberData(nameof(ProtectedEndpoints))]
        public async Task ProtectedEndpoint_WithoutCredentials_Returns401AndCallsNoService(
            string method,
            string path,
            string? body)
        {
            Mock<ITrackingService> service = new(MockBehavior.Strict);
            await using WebApplicationFactory<Program> factory = CreateFactory(service);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.SendAsync(
                CreateRequest(method, path, body),
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            service.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("POST", "/api/Tracking/start", "{\"jobId\":\"job-1\",\"driverId\":7}", "Driver")]
        [InlineData("POST", "/api/Tracking/stop/job-1", null, "Driver")]
        [InlineData("GET", "/api/Tracking/location/job-1", null, "Admin")]
        [InlineData("GET", "/api/Tracking/history/job-1", null, "Manager")]
        public async Task ProtectedEndpoint_WithAnAllowedRole_IsNotRejected(
            string method,
            string path,
            string? body,
            string role)
        {
            Mock<ITrackingService> service = new();
            service.Setup(s => s.StartTrackingAsync(s_jobId, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            service.Setup(s => s.StopTrackingAsync(s_jobId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            Mock<IJobService> jobs = new();
            jobs.Setup(s => s.GetJobByIdAsync(s_jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Job { Id = s_jobId, AssignedDriverId = 7, Status = JobStatus.Assigned });
            await using WebApplicationFactory<Program> factory = CreateFactory(service, jobs);
            using HttpClient client = factory.CreateAuthenticatedClient(role);
            if (role == "Driver")
            {
                client.DefaultRequestHeaders.Add("X-Test-Driver-Id", "7");
            }

            using HttpResponseMessage response = await client.SendAsync(
                CreateRequest(method, path, body),
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized).And.NotBe(HttpStatusCode.Forbidden);
        }

        [Theory]
        [InlineData("GET", "/api/Tracking/location/job-1", "Driver")]
        [InlineData("GET", "/api/Tracking/history/job-1", "Driver")]
        [InlineData("POST", "/api/Tracking/start", "Customer")]
        public async Task ProtectedEndpoint_WithADisallowedRole_Returns403(string method, string path, string role)
        {
            Mock<ITrackingService> service = new(MockBehavior.Strict);
            await using WebApplicationFactory<Program> factory = CreateFactory(service);
            using HttpClient client = factory.CreateAuthenticatedClient(role);

            using HttpResponseMessage response = await client.SendAsync(
                CreateRequest(method, path, "{\"jobId\":\"job-1\",\"driverId\":7}"),
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            service.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetPublicTracking_WithoutCredentials_ReturnsOnlyPublicFields()
        {
            Mock<ITrackingService> service = new();
            service.Setup(s => s.GetCurrentLocationAsync(s_jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocationTracking(s_jobId, 4242, 40.5m, -74.25m)
                {
                    Id = 9001,
                    Speed = 61m,
                    Heading = 90,
                    Accuracy = 5m,
                });
            Mock<IJobService> jobs = new();
            jobs.Setup(s => s.GetJobByIdAsync(s_jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Job
                {
                    Id = s_jobId,
                    Status = JobStatus.EnRoute,
                    EstimatedDeliveryTime = new DateTime(2026, 11, 20, 16, 0, 0, DateTimeKind.Utc)
                });
            await using WebApplicationFactory<Program> factory = CreateFactory(service, jobs);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync(
                "/api/Tracking/public/job-1",
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            JsonElement payload = JsonDocument.Parse(raw).RootElement;
            payload.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(s_publicFields);
            raw.Should().NotContain("4242", "the driver id is not public").And.NotContain("9001");
        }

        [Fact]
        public async Task GetPublicTracking_WhenNothingIsTracked_Returns404WithoutCredentials()
        {
            Mock<ITrackingService> service = new();
            service.Setup(s => s.GetCurrentLocationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocationTracking?)null);
            await using WebApplicationFactory<Program> factory = CreateFactory(service);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync(
                "/api/Tracking/public/job-1",
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task StartTracking_WhenADriverNamesAnotherDriver_Returns403AndCallsNoService()
        {
            Mock<ITrackingService> service = new(MockBehavior.Strict);
            await using WebApplicationFactory<Program> factory = CreateFactory(service);
            using HttpClient client = factory.CreateAuthenticatedClient("Driver");
            client.DefaultRequestHeaders.Add("X-Test-Driver-Id", "7");

            using HttpResponseMessage response = await client.SendAsync(
                CreateRequest("POST", "/api/Tracking/start", "{\"jobId\":\"job-1\",\"driverId\":8}"),
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            service.VerifyNoOtherCalls();
        }

        private static HttpRequestMessage CreateRequest(string method, string path, string? body)
        {
            HttpRequestMessage request = new(new HttpMethod(method), path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(JsonDocument.Parse(body).RootElement);
            }

            return request;
        }

        private static WebApplicationFactory<Program> CreateFactory(
            Mock<ITrackingService> service,
            Mock<IJobService>? jobs = null)
        {
            return ApiHostFactory.Create(
                new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = string.Empty },
                services =>
                {
                    services.ReplaceScoped(service.Object);
                    if (jobs is not null)
                    {
                        services.ReplaceScoped(jobs.Object);
                    }
                });
        }
    }
}
