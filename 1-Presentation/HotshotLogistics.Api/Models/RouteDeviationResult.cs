// <copyright file="RouteDeviationResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Result model for route deviation checks.
    /// </summary>
    public sealed record RouteDeviationResult
    {
        /// <summary>
        ///     Gets the job ID.
        /// </summary>
        public string JobId { get; init; } = string.Empty;

        /// <summary>
        ///     Gets a value indicating whether the driver has deviated from route.
        /// </summary>
        public bool HasDeviated { get; init; }

        /// <summary>
        ///     Gets the current location.
        /// </summary>
        public LocationUpdate CurrentLocation { get; init; } = new();

        /// <summary>
        ///     Gets the check timestamp.
        /// </summary>
        public DateTime CheckedAt { get; init; }

        /// <summary>
        ///     Gets the result message.
        /// </summary>
        public string Message { get; init; } = string.Empty;
    }
}
