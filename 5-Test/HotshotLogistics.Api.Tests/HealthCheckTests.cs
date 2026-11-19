using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Proves the liveness, readiness and full health endpoints. The database is made
    ///     unreachable explicitly so the result does not depend on the machine's user secrets.
    /// </summary>
    public class HealthCheckTests
    {
        private const string s_unreachableHost = "198.51.100.1";

        private static readonly Dictionary<string, string?> s_unreachableDatabase = new()
        {
            // 198.51.100.0/24 is reserved for documentation (RFC 5737) and never routes.
            ["ConnectionStrings:DefaultConnection"] =
                $"Server=tcp:{s_unreachableHost},1433;Database=leak-sentinel-db;Integrated Security=True;Connect Timeout=1;Encrypt=False",
            ["Health:Database:TimeoutSeconds"] = "1",
        };

        private static readonly Dictionary<string, string?> s_missingConnectionString = new()
        {
            ["ConnectionStrings:DefaultConnection"] = string.Empty,
        };

        [Fact]
        public async Task GetLive_WhenDatabaseIsDown_ReturnsHealthy()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(s_unreachableDatabase);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response =
                await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadStatusAsync(response)).Should().Be("Healthy");
        }

        [Fact]
        public async Task GetReady_WhenDatabaseIsUnreachable_Returns503WithoutLeakingDetails()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(s_unreachableDatabase);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response =
                await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            raw.Should().NotContain(s_unreachableHost).And.NotContain("leak-sentinel-db").And.NotContain("SqlException");
            JsonDocument.Parse(raw).RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
        }

        [Fact]
        public async Task GetReady_WhenConnectionStringIsMissing_Returns503()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(s_missingConnectionString);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response =
                await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task GetHealth_WhenDatabaseIsUnreachable_ListsTheDatabaseCheckAsUnhealthy()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(s_unreachableDatabase);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response =
                await client.GetAsync("/health", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            JsonElement check = JsonDocument.Parse(raw).RootElement.GetProperty("checks")[0];
            check.GetProperty("name").GetString().Should().Be("database");
            check.GetProperty("status").GetString().Should().Be("Unhealthy");
        }

        [Theory]
        [InlineData("/health")]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public async Task GetHealthEndpoints_WithoutCredentials_AreNotChallenged(string path)
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(s_unreachableDatabase);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        }

        private static async Task<string?> ReadStatusAsync(HttpResponseMessage response)
        {
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            return JsonDocument.Parse(raw).RootElement.GetProperty("status").GetString();
        }
    }
}
