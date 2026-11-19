// <copyright file="CorrelationIdMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace HotshotLogistics.Api.Middleware
{
    /// <summary>
    ///     Accepts the caller's correlation id (or creates one), echoes it on the response, and puts
    ///     it on the log scope so logs, traces and <c>ProblemDetails</c> bodies share one id.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        /// <summary>
        ///     The request and response header that carries the correlation id.
        /// </summary>
        public const string HeaderName = "X-Correlation-ID";

        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly RequestDelegate _next;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CorrelationIdMiddleware" /> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Gets the correlation id the middleware assigned to the request, if it ran.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The correlation id, or <c>null</c> when the middleware did not run.</returns>
        public static string? GetCorrelationId(HttpContext context)
        {
            return context.Items.TryGetValue(HeaderName, out object? value) ? value as string : null;
        }

        /// <summary>
        ///     Reads the correlation id from the request header, or creates one, and runs the rest of the pipeline.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            string correlationId = context.Request.Headers.TryGetValue(HeaderName, out StringValues supplied) &&
                                   !StringValues.IsNullOrEmpty(supplied)
                ? supplied.ToString()
                : Guid.NewGuid().ToString();

            context.Items[HeaderName] = correlationId;
            Activity.Current?.SetTag("correlation.id", correlationId);
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                await _next(context);
            }
        }
    }
}
