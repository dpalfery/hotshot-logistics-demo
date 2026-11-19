using System.Net;
using System.Text.Json;
using FluentAssertions;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Exceptions;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Proves every error path answers with an RFC 9457 <c>application/problem+json</c> body.
    /// </summary>
    public class ProblemDetailsTests
    {
        private const string s_problemContentType = "application/problem+json";
        private const string s_driversEndpoint = "/api/Drivers";
        private const string s_secretMessage = "connection string Server=secret;Password=hunter2";

        public static TheoryData<Exception, HttpStatusCode> DomainExceptions => new()
        {
            { new ValidationException("Validation failed"), HttpStatusCode.BadRequest },
            { new BusinessRuleException("Rule broken"), HttpStatusCode.UnprocessableEntity },
            { new PaymentProcessingException("Card declined", "txn-1", "declined"), HttpStatusCode.PaymentRequired },
            { new ExternalServiceException("Maps", "Maps is down", 503), HttpStatusCode.BadGateway },
            { new InvalidOperationException(s_secretMessage), HttpStatusCode.InternalServerError },
        };

        [Theory]
        [MemberData(nameof(DomainExceptions))]
        public async Task GetDrivers_WhenServiceThrows_ReturnsProblemDetailsForTheExceptionKind(
            Exception exception,
            HttpStatusCode expectedStatus)
        {
            Mock<IDriverService> driverService = new();
            driverService.Setup(s => s.GetDriversAsync()).ThrowsAsync(exception);
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(
                configureServices: services => services.ReplaceScoped(driverService.Object));
            using HttpClient client = factory.CreateAuthenticatedClient();

            using HttpResponseMessage response =
                await client.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(expectedStatus);
            response.Content.Headers.ContentType?.MediaType.Should().Be(s_problemContentType);
            JsonElement body = await ReadBodyAsync(response);
            body.GetProperty("status").GetInt32().Should().Be((int)expectedStatus);
            body.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
            body.TryGetProperty("traceId", out _).Should().BeTrue("every problem body carries the trace id");
            body.TryGetProperty("correlationId", out _).Should().BeTrue("every problem body carries the correlation id");
        }

        [Fact]
        public async Task GetDrivers_WhenServiceThrowsUnexpectedly_DoesNotLeakInternals()
        {
            Mock<IDriverService> driverService = new();
            driverService.Setup(s => s.GetDriversAsync()).ThrowsAsync(new InvalidOperationException(s_secretMessage));
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(
                configureServices: services => services.ReplaceScoped(driverService.Object));
            using HttpClient client = factory.CreateAuthenticatedClient();

            using HttpResponseMessage response =
                await client.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);

            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            raw.Should().NotContain("hunter2").And.NotContain("StackTrace").And.NotContain("   at ");
        }

        [Fact]
        public async Task GetDrivers_WhenValidationExceptionHasFieldErrors_ReturnsThemAsValidationProblem()
        {
            Mock<IDriverService> driverService = new();
            driverService.Setup(s => s.GetDriversAsync()).ThrowsAsync(new ValidationException(
                "Validation failed",
                new Dictionary<string, string[]> { ["Title"] = ["Title is required"] }));
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(
                configureServices: services => services.ReplaceScoped(driverService.Object));
            using HttpClient client = factory.CreateAuthenticatedClient();

            using HttpResponseMessage response =
                await client.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);

            JsonElement body = await ReadBodyAsync(response);
            body.GetProperty("errors").GetProperty("Title")[0].GetString().Should().Be("Title is required");
        }

        [Fact]
        public async Task GetDriverById_WhenDriverMissing_ReturnsNotFoundProblemWithDetail()
        {
            Mock<IDriverService> driverService = new();
            driverService.Setup(s => s.GetDriverByIdAsync(42)).ReturnsAsync((Driver?)null);
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(
                configureServices: services => services.ReplaceScoped(driverService.Object));
            using HttpClient client = factory.CreateAuthenticatedClient();

            using HttpResponseMessage response =
                await client.GetAsync($"{s_driversEndpoint}/42", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Content.Headers.ContentType?.MediaType.Should().Be(s_problemContentType);
            JsonElement body = await ReadBodyAsync(response);
            body.GetProperty("detail").GetString().Should().Contain("42");
            body.TryGetProperty("traceId", out _).Should().BeTrue();
        }

        [Fact]
        public async Task PostDriver_WithMalformedBody_ReturnsValidationProblem()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create();
            using HttpClient client = factory.CreateAuthenticatedClient();
            using StringContent content = new("{\"id\": \"not-a-number\"}", System.Text.Encoding.UTF8, "application/json");

            using HttpResponseMessage response =
                await client.PostAsync(s_driversEndpoint, content, TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType?.MediaType.Should().Be(s_problemContentType);
            JsonElement body = await ReadBodyAsync(response);
            body.TryGetProperty("errors", out _).Should().BeTrue();
        }

        [Fact]
        public async Task GetUnknownRoute_ReturnsNotFoundProblem()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create();
            using HttpClient client = factory.CreateAuthenticatedClient();

            using HttpResponseMessage response =
                await client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

            await AssertProblemAsync(response, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task PatchDrivers_WithUnsupportedMethod_ReturnsMethodNotAllowedProblem()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create();
            using HttpClient client = factory.CreateAuthenticatedClient();
            using HttpRequestMessage request = new(HttpMethod.Patch, s_driversEndpoint);

            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            await AssertProblemAsync(response, HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public async Task GetDrivers_WithoutCredentials_ReturnsUnauthorizedProblem()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create();
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response =
                await client.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);

            await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDrivers_AsDriverRole_ReturnsForbiddenProblem()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create();
            using HttpClient client = factory.CreateAuthenticatedClient("Driver");

            using HttpResponseMessage response =
                await client.GetAsync(s_driversEndpoint, TestContext.Current.CancellationToken);

            await AssertProblemAsync(response, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetDrivers_WithCorrelationIdHeader_EchoesItInHeaderAndBody()
        {
            Mock<IDriverService> driverService = new();
            driverService.Setup(s => s.GetDriversAsync()).ThrowsAsync(new BusinessRuleException("Rule broken"));
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(
                configureServices: services => services.ReplaceScoped(driverService.Object));
            using HttpClient client = factory.CreateAuthenticatedClient();
            using HttpRequestMessage request = new(HttpMethod.Get, s_driversEndpoint);
            request.Headers.Add("X-Correlation-ID", "corr-123");

            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.Headers.GetValues("X-Correlation-ID").Should().ContainSingle().Which.Should().Be("corr-123");
            JsonElement body = await ReadBodyAsync(response);
            body.GetProperty("correlationId").GetString().Should().Be("corr-123");
        }

        private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
        {
            response.StatusCode.Should().Be(expectedStatus);
            response.Content.Headers.ContentType?.MediaType.Should().Be(s_problemContentType);
            JsonElement body = await ReadBodyAsync(response);
            body.GetProperty("status").GetInt32().Should().Be((int)expectedStatus);
        }

        private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
        {
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            return JsonDocument.Parse(raw).RootElement.Clone();
        }
    }
}
