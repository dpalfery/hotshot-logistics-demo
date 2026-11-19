// <copyright file="RateLimitingOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace HotshotLogistics.Api.RateLimiting
{
    /// <summary>
    ///     Request limits, bound from <c>RateLimiting</c>. Each limit is a fixed window per client
    ///     address. The defaults are generous because every client behind one proxy shares an address
    ///     until forwarded headers are configured.
    /// </summary>
    public sealed class RateLimitingOptions
    {
        /// <summary>
        ///     The configuration section these options bind from.
        /// </summary>
        public const string SectionName = "RateLimiting";

        /// <summary>
        ///     Gets or sets how many requests one client address may make per global window.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int GlobalPermitLimit { get; set; } = 1000;

        /// <summary>
        ///     Gets or sets the length, in seconds, of the global window.
        /// </summary>
        [Range(1, 86400)]
        public int GlobalWindowSeconds { get; set; } = 60;

        /// <summary>
        ///     Gets or sets how many requests one client address may make per window to an
        ///     unauthenticated endpoint (public tracking and payment webhooks).
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PublicPermitLimit { get; set; } = 60;

        /// <summary>
        ///     Gets or sets the length, in seconds, of the public endpoint window.
        /// </summary>
        [Range(1, 86400)]
        public int PublicWindowSeconds { get; set; } = 60;
    }
}
