using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Builds the real API pipeline in memory for platform tests. Settings given here are
    ///     applied last, so a test overrides anything user secrets or appsettings supply on the
    ///     machine that runs it.
    /// </summary>
    internal static class ApiHostFactory
    {
        public static WebApplicationFactory<Program> Create(
            IDictionary<string, string?>? settings = null,
            Action<IServiceCollection>? configureServices = null,
            string environment = "Development")
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

                if (configureServices is not null)
                {
                    builder.ConfigureTestServices(configureServices);
                }
            });
        }

        /// <summary>
        ///     Replaces a registered service with a test double.
        /// </summary>
        public static void ReplaceScoped<TService>(this IServiceCollection services, TService instance)
            where TService : class
        {
            services.RemoveAll<TService>();
            services.AddScoped(_ => instance);
        }

        /// <summary>
        ///     Replaces a registered service with a singleton test double.
        /// </summary>
        public static void ReplaceSingleton<TService>(this IServiceCollection services, TService instance)
            where TService : class
        {
            services.RemoveAll<TService>();
            services.AddSingleton(instance);
        }

        /// <summary>
        ///     Creates a client that authenticates through the API's development <c>Test</c> scheme.
        /// </summary>
        public static HttpClient CreateAuthenticatedClient(
            this WebApplicationFactory<Program> factory,
            string role = "Admin")
        {
            HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Add("X-Test-Role", role);
            return client;
        }
    }
}
