// <copyright file="PublicTrackingInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Public tracking information for customer access.
    /// </summary>
    public sealed record PublicTrackingInfo
    {
        /// <summary>
        ///     Gets the job ID.
        /// </summary>
        public string JobId { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the current latitude.
        /// </summary>
        public double CurrentLatitude { get; init; }

        /// <summary>
        ///     Gets the current longitude.
        /// </summary>
        public double CurrentLongitude { get; init; }

        /// <summary>
        ///     Gets the last updated timestamp.
        /// </summary>
        public DateTime LastUpdated { get; init; }

        /// <summary>
        ///     Gets the current status.
        /// </summary>
        public string Status { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the estimated arrival time.
        /// </summary>
        public DateTime? EstimatedArrival { get; init; }
    }
}
