using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using HotshotLogistics.Application.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Exercises the API's declared authentication and authorization behaviour by hosting the
    ///     real application pipeline in memory.
    ///     <para>
    ///         Development hosting uses the API's own <c>Test</c> scheme, which is the scheme
    ///         <c>Program</c> registers for local development, so a request with no credentials or
    ///         with a foreign scheme is rejected by the authorization gate before any controller
    ///         runs. That keeps these tests free of a database or any other external service.
    ///     </para>
    /// </summary>
    public class AuthenticationTests
    {
        private const string s_driversEndpoint = "/api/Drivers";
        private const string s_swaggerDocumentEndpoint = "/swagger/v1/swagger.json";
        private const string s_roleHeader = "X-Test-Role";
        private const string s_testScheme = "Test";

        [Fact]
        public async Task GetDrivers_WithoutAuthorizationHeader_Returns401()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory("Development");
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync(
                s_driversEndpoint,
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDrivers_WithInvalidBearerToken_Returns401()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory("Development");
            using HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            using HttpResponseMessage response = await client.GetAsync(
                s_driversEndpoint,
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetDrivers_WithTestSchemeAndDriverRole_Returns403()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory("Development");
            using HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(s_testScheme);
            client.DefaultRequestHeaders.Add(s_roleHeader, AuthorizationPolicies.Driver);

            using HttpResponseMessage response = await client.GetAsync(
                s_driversEndpoint,
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(
                HttpStatusCode.Forbidden,
                "the Drivers endpoint requires the ManagerOrAdmin policy and a Driver is only authenticated");
        }

        [Fact]
        public async Task GetSwaggerDocument_WithoutCredentials_Returns200()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory("Development");
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync(
                s_swaggerDocumentEndpoint,
                TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(
                HttpStatusCode.OK,
                "the host must serve an endpoint outside the gate, which proves the 401 responses above come from authorization and not from a broken pipeline");
        }

        [Fact]
        public async Task AuthenticationScheme_WhenHostedInDevelopment_UsesTestScheme()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory("Development");
            IAuthenticationSchemeProvider schemes = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();

            AuthenticationScheme? defaultScheme =
                await schemes.GetDefaultAuthenticateSchemeAsync();

            defaultScheme.Should().NotBeNull();
            defaultScheme!.Name.Should().Be(s_testScheme);
        }

        [Fact]
        public async Task AuthenticationScheme_WhenHostedOutsideDevelopment_UsesJwtBearerScheme()
        {
            await using WebApplicationFactory<Program> factory =
                CreateFactory("Production", CreateIdentityProviderSettings());
            IAuthenticationSchemeProvider schemes = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();

            AuthenticationScheme? defaultScheme =
                await schemes.GetDefaultAuthenticateSchemeAsync();

            defaultScheme.Should().NotBeNull();
            defaultScheme!.Name.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        }

        [Fact]
        public async Task AuthorizationPolicy_ManagerOrAdmin_RequiresAdminOrManagerRole()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory("Development");
            IAuthorizationPolicyProvider policyProvider =
                factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();

            AuthorizationPolicy? policy =
                await policyProvider.GetPolicyAsync(AuthorizationPolicies.ManagerOrAdmin);

            policy.Should().NotBeNull();
            RolesAuthorizationRequirement requirement =
                policy!.Requirements.OfType<RolesAuthorizationRequirement>().Should().ContainSingle().Subject;
            requirement.AllowedRoles.Should().BeEquivalentTo(
                new[] { AuthorizationPolicies.Admin, AuthorizationPolicies.Manager });
        }

        private static WebApplicationFactory<Program> CreateFactory(
            string environment,
            IDictionary<string, string?>? settings = null)
        {
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Mapping:Provider"] = "Mock",
                    });

                    if (settings is not null)
                    {
                        configuration.AddInMemoryCollection(settings);
                    }
                });
            });
        }

        /// <summary>
        ///     Placeholder identity-provider values so the non-Development JWT bearer registration can
        ///     bind. They are RFC 2606 reserved names and all-zero GUIDs, never real credentials, and no
        ///     request is issued against them.
        /// </summary>
        private static Dictionary<string, string?> CreateIdentityProviderSettings()
        {
            return new Dictionary<string, string?>
            {
                ["AzureAdB2C:Instance"] = "https://example.invalid/",
                ["AzureAdB2C:Domain"] = "example.invalid",
                ["AzureAdB2C:TenantId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAdB2C:ClientId"] = "00000000-0000-0000-0000-000000000000",
                ["AzureAdB2C:SignUpSignInPolicyId"] = "B2C_1_signupsignin",
            };
        }
    }
}
