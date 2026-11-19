// <copyright file="RateLimitingExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Globalization;
using System.Threading.RateLimiting;
using HotshotLogistics.Api.Health;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace HotshotLogistics.Api.RateLimiting
{
    /// <summary>
    ///     Registers the global limit and the named per-endpoint policies.
    /// </summary>
    public static class RateLimitingExtensions
    {
        /// <summary>
        ///     Adds rate limiting: a global fixed-window limit per client address, a stricter
        ///     <see cref="RateLimitPolicies.Public" /> policy, and a 429 problem response with
        ///     <c>Retry-After</c>. Health endpoints are exempt from the global limit so an
        ///     orchestrator is never throttled out of probing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddApiRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<RateLimitingOptions>()
                .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddRateLimiter(_ => { });
            services.AddOptions<RateLimiterOptions>()
                .Configure<IOptions<RateLimitingOptions>>((limiter, settings) =>
                {
                    RateLimitingOptions limits = settings.Value;
                    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                    limiter.OnRejected = WriteRejectionAsync;
                    limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                        context.Request.Path.StartsWithSegments(HealthCheckExtensions.HealthPath)
                            ? RateLimitPartition.GetNoLimiter("health")
                            : RateLimitPartition.GetFixedWindowLimiter(
                                ClientKey(context),
                                _ => FixedWindow(limits.GlobalPermitLimit, limits.GlobalWindowSeconds)));
                    limiter.AddPolicy(RateLimitPolicies.Public, context =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            ClientKey(context),
                            _ => FixedWindow(limits.PublicPermitLimit, limits.PublicWindowSeconds)));
                });
            return services;
        }

        private static string ClientKey(HttpContext context)
        {
            // The in-memory test host has no remote address; every such request shares one partition.
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static FixedWindowRateLimiterOptions FixedWindow(int permitLimit, int windowSeconds)
        {
            return new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            };
        }

        private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
        {
            HttpContext httpContext = context.HttpContext;
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
            {
                httpContext.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
            }

            IProblemDetailsService problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
            await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails =
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests.",
                    Detail = "The request rate limit was exceeded. Retry after the delay in the Retry-After header."
                }
            });
        }
    }
}
