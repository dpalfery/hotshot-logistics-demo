using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace HotshotLogistics.IntegrationTests
{
    [Collection(DatabaseCollection.Name)]
    public class AuthenticationAndAuthorizationTests : IntegrationTestBase
    {
        public AuthenticationAndAuthorizationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetDrivers_WithoutAuthorizationHeader_Returns401()
        {
            using HttpClient client = Factory.CreateClient();
            HttpResponseMessage response = await client.GetAsync("/api/Drivers");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDrivers_WithDriverRole_Returns403()
        {
            using HttpClient client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "Driver");

            HttpResponseMessage response = await client.GetAsync("/api/Drivers");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetDrivers_WithAdminRole_Returns200()
        {
            using HttpClient client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            HttpResponseMessage response = await client.GetAsync("/api/Drivers");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetOverdueInvoices_WithAdminRole_Returns200()
        {
            using HttpClient client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

            HttpResponseMessage response = await client.GetAsync("/api/Billing/invoices/overdue");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetOverdueInvoices_WithDriverRole_Returns403()
        {
            using HttpClient client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Remove("X-Test-Role");
            client.DefaultRequestHeaders.Add("X-Test-Role", "Driver");

            HttpResponseMessage response = await client.GetAsync("/api/Billing/invoices/overdue");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Request_WithInvalidAuthorizationScheme_Returns401()
        {
            using HttpClient client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            HttpResponseMessage response = await client.GetAsync("/api/Drivers");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
