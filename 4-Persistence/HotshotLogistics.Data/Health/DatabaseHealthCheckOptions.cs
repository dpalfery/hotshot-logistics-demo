// <copyright file="DatabaseHealthCheckOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Data.Health
{
    /// <summary>
    ///     Settings for <see cref="DatabaseHealthCheck" />, bound from <c>Health:Database</c>.
    /// </summary>
    public sealed class DatabaseHealthCheckOptions
    {
        /// <summary>
        ///     The configuration section these options bind from.
        /// </summary>
        public const string SectionName = "Health:Database";

        /// <summary>
        ///     Gets or sets how long, in seconds, the check may take to connect and answer before the
        ///     database counts as unhealthy.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 3;
    }
}
