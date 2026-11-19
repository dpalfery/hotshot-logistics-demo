// <copyright file="ErrorHandlingExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Api.Middleware;

namespace HotshotLogistics.Api.Errors
{
    /// <summary>
    ///     Registers and applies the API's single error contract: RFC 9457 problem responses with a
    ///     trace id and correlation id on every error body.
    /// </summary>
    public static class ErrorHandlingExtensions
    {
        /// <summary>
        ///     Adds <c>ProblemDetails</c> and the domain exception handler to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddApiErrorHandling(this IServiceCollection services)
        {
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    string? correlationId = CorrelationIdMiddleware.GetCorrelationId(context.HttpContext);
                    if (correlationId is not null)
                    {
                        context.ProblemDetails.Extensions["correlationId"] = correlationId;
                    }
                };
            });
            services.AddExceptionHandler<DomainExceptionHandler>();
            return services;
        }

        /// <summary>
        ///     Adds the correlation id, exception handler and status-code pages to the pipeline. Call it
        ///     first so every later middleware's failures use the error contract.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseApiErrorHandling(this IApplicationBuilder app)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseExceptionHandler();
            app.UseStatusCodePages();
            return app;
        }
    }
}
