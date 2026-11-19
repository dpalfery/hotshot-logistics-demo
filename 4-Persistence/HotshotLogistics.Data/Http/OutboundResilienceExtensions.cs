// <copyright file="OutboundResilienceExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace HotshotLogistics.Data.Http
{
    /// <summary>
    ///     Gives every outbound HTTP client the same timeout, retry and circuit-breaker pipeline.
    /// </summary>
    public static class OutboundResilienceExtensions
    {
        /// <summary>
        ///     The configuration section that overrides the pipeline's defaults.
        /// </summary>
        public const string SectionName = "Resilience:Outbound";

        /// <summary>
        ///     Adds the standard resilience pipeline to an HTTP client registration: a total request
        ///     timeout, bounded retries with exponential backoff on transient failures (5xx, 408, 429, network errors
        ///     and attempt timeouts; never on caller cancellation), a per-attempt timeout and a circuit breaker.
        ///     The settings bind from <c>Resilience:Outbound</c> and the framework rejects inconsistent values at
        ///     client creation.
        /// </summary>
        /// <param name="builder">The HTTP client builder.</param>
        /// <returns>The HTTP client builder for chaining.</returns>
        public static IHttpClientBuilder AddOutboundResilience(this IHttpClientBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            IHttpStandardResiliencePipelineBuilder pipeline = builder.AddStandardResilienceHandler();
            pipeline.Services.AddOptions<HttpStandardResilienceOptions>(pipeline.PipelineName)
                .Configure<IConfiguration>((options, configuration) =>
                    configuration.GetSection(SectionName).Bind(options));
            return builder;
        }
    }
}
